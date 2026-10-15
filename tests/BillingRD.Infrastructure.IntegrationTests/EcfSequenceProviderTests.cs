using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillingRD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BillingRD.Infrastructure.IntegrationTests;

public sealed class EcfSequenceProviderTests
{
    [Fact]
    public async Task Allocations_are_idempotent_and_isolated_by_environment()
    {
        var connectionString = Environment.GetEnvironmentVariable("BILLINGRD_TEST_DB")
            ?? throw new InvalidOperationException("BILLINGRD_TEST_DB is required.");

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:BillingDatabase", connectionString);
                builder.UseEnvironment("Testing");
            });

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/auth/register", new
            {
                email = "sequences@example.test",
                password = "StrongPass123!"
            })).StatusCode);

        var business = await client.PostAsJsonAsync("/api/businesses/", new { name = "Sequence Test" });
        var branchId = (await business.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("branchId").GetGuid();

        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync("/api/fiscal-profile/", new
            {
                rnc = "101654321",
                legalName = "Sequence Test SRL",
                tradeName = "Sequence Test",
                address = "Calle Secuencia 1"
            })).StatusCode);

        var customer = await client.PostAsJsonAsync("/api/customers/", new
        {
            name = "Cliente Secuencia SRL",
            taxId = "101123456",
            fiscalAddress = "Calle Cliente 2"
        });
        var customerId = (await customer.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var product = await client.PostAsJsonAsync("/api/products/", new
        {
            name = "Servicio fiscal",
            sku = "SERV-SEQ",
            salePrice = 100m,
            itbisCategory = "Standard",
            kind = "Service",
            tracksInventory = false
        });
        var productId = (await product.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        async Task<Guid> CreateDraftAsync(string idempotencyKey)
        {
            using var saleRequest = new HttpRequestMessage(HttpMethod.Post, "/api/sales/");
            saleRequest.Headers.Add("Idempotency-Key", idempotencyKey);
            saleRequest.Content = JsonContent.Create(new
            {
                branchId,
                customerId,
                items = new[] { new { productId, quantity = 1m } },
                payments = new[] { new { method = "Card", amount = 118m, reference = idempotencyKey } }
            });

            var saleResponse = await client.SendAsync(saleRequest);
            Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);
            var invoiceId = (await saleResponse.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("invoice").GetProperty("id").GetGuid();

            var draftResponse = await client.PostAsJsonAsync(
                $"/api/electronic-invoicing/drafts/invoice/{invoiceId}",
                new { type = "CreditFiscalInvoice31" });

            Assert.Equal(HttpStatusCode.Created, draftResponse.StatusCode);
            return (await draftResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }

        var preCertRange = await client.PostAsJsonAsync(
            "/api/electronic-invoicing/sequence-ranges/",
            new
            {
                environment = "PreCertification",
                type = "CreditFiscalInvoice31",
                fromNumber = 1,
                toNumber = 2,
                nextNumber = 1,
                expirationDate = "2028-12-31"
            });
        Assert.Equal(HttpStatusCode.Created, preCertRange.StatusCode);

        var productionRange = await client.PostAsJsonAsync(
            "/api/electronic-invoicing/sequence-ranges/",
            new
            {
                environment = "Production",
                type = "CreditFiscalInvoice31",
                fromNumber = 1,
                toNumber = 2,
                nextNumber = 1,
                expirationDate = "2028-12-31"
            });
        Assert.Equal(HttpStatusCode.Created, productionRange.StatusCode);

        var preCertDraftId = await CreateDraftAsync("sequence-sale-precert");
        var productionDraftId = await CreateDraftAsync("sequence-sale-production");

        var firstAllocation = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/{preCertDraftId}/e-ncf",
            new { environment = "PreCertification" });
        Assert.Equal(HttpStatusCode.Created, firstAllocation.StatusCode);

        var firstJson = await firstAllocation.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("E310000000001", firstJson.GetProperty("eNcf").GetString());
        Assert.Equal("PreCertification", firstJson.GetProperty("environment").GetString());
        Assert.False(firstJson.GetProperty("issued").GetBoolean());
        Assert.False(firstJson.GetProperty("digitallySigned").GetBoolean());
        Assert.False(firstJson.GetProperty("submittedToDgii").GetBoolean());

        var retryAllocation = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/{preCertDraftId}/e-ncf",
            new { environment = "PreCertification" });
        Assert.Equal(HttpStatusCode.OK, retryAllocation.StatusCode);
        Assert.Equal(
            "E310000000001",
            (await retryAllocation.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("eNcf").GetString());

        var productionAllocation = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/{productionDraftId}/e-ncf",
            new { environment = "Production" });
        Assert.Equal(HttpStatusCode.Created, productionAllocation.StatusCode);

        var productionJson = await productionAllocation.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("E310000000001", productionJson.GetProperty("eNcf").GetString());
        Assert.Equal("Production", productionJson.GetProperty("environment").GetString());

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<BillingDbContext>();

        Assert.Equal(2, await verificationDb.EcfSequenceAllocations.CountAsync());
        Assert.Equal(2, await verificationDb.EcfSequenceRanges.CountAsync());

        var nextNumbers = await verificationDb.EcfSequenceRanges
            .OrderBy(x => x.Environment)
            .Select(x => x.NextNumber)
            .ToListAsync();

        Assert.All(nextNumbers, next => Assert.Equal(2, next));
    }
}
