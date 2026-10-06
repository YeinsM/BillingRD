using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillingRD.Domain.Adjustments;
using BillingRD.Domain.ElectronicInvoicing;
using BillingRD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BillingRD.Infrastructure.IntegrationTests;

public sealed class ElectronicInvoicingFoundationTests
{
    [Fact]
    public async Task Return_creates_internal_adjustment_and_one_non_issued_credit_note_draft()
    {
        var connectionString = Environment.GetEnvironmentVariable("BILLINGRD_TEST_DB")
            ?? throw new InvalidOperationException("BILLINGRD_TEST_DB is required for integration tests.");

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:BillingDatabase", connectionString);
                builder.UseEnvironment("Testing");
            });

        await ResetDatabaseAsync(factory);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        Assert.Equal(
            HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/auth/register", new
            {
                email = "ecf-owner@example.test",
                password = "StrongPass123!"
            })).StatusCode);

        var businessResponse = await client.PostAsJsonAsync("/api/businesses/", new { name = "e-CF Foundation" });
        Assert.Equal(HttpStatusCode.Created, businessResponse.StatusCode);
        var businessJson = await businessResponse.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = businessJson.GetProperty("branchId").GetGuid();

        var productResponse = await client.PostAsJsonAsync("/api/products/", new
        {
            name = "Servicio para nota",
            sku = "ECF-001",
            salePrice = 100m,
            itbisCategory = "Standard",
            tracksInventory = false
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var saleRequest = new HttpRequestMessage(HttpMethod.Post, "/api/sales/");
        saleRequest.Headers.Add("Idempotency-Key", "ecf-source-sale");
        saleRequest.Content = JsonContent.Create(new
        {
            branchId,
            customerId = (Guid?)null,
            items = new[] { new { productId, quantity = 1m } },
            payments = new[] { new { method = "Card", amount = 118m, reference = "AUTH-ECF" } }
        });

        var saleResponse = await client.SendAsync(saleRequest);
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);
        var saleId = (await saleResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        using var returnRequest = new HttpRequestMessage(HttpMethod.Post, "/api/returns/");
        returnRequest.Headers.Add("Idempotency-Key", "ecf-return-001");
        returnRequest.Content = JsonContent.Create(new
        {
            saleId,
            reason = "Devolución que requiere ajuste interno",
            items = new[] { new { productId, quantity = 1m } },
            refunds = new[] { new { method = "Card", amount = 118m, reference = "REF-ECF" } }
        });

        var returnResponse = await client.SendAsync(returnRequest);
        Assert.Equal(HttpStatusCode.Created, returnResponse.StatusCode);
        var returnJson = await returnResponse.Content.ReadFromJsonAsync<JsonElement>();
        var returnId = returnJson.GetProperty("id").GetGuid();

        Guid adjustmentId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

            var adjustment = await db.AdjustmentDocuments.IgnoreQueryFilters().SingleAsync();
            adjustmentId = adjustment.Id;

            Assert.Equal(saleId, adjustment.SaleId);
            Assert.Equal(returnId, adjustment.ReturnId);
            Assert.Equal(AdjustmentKind.Credit, adjustment.Kind);
            Assert.Equal(100m, adjustment.Subtotal);
            Assert.Equal(18m, adjustment.TaxAmount);
            Assert.Equal(118m, adjustment.Total);
            Assert.Equal(0, await db.ElectronicFiscalDocumentDrafts.IgnoreQueryFilters().CountAsync());
        }

        var adjustmentResponse = await client.GetAsync($"/api/adjustments/{adjustmentId}");
        Assert.Equal(HttpStatusCode.OK, adjustmentResponse.StatusCode);
        var adjustmentJson = await adjustmentResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Credit", adjustmentJson.GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, adjustmentJson.GetProperty("electronicFiscalDraftId").ValueKind);

        var createDraft = await client.PostAsync(
            $"/api/electronic-invoicing/drafts/credit-note/from-adjustment/{adjustmentId}",
            content: null);

        Assert.Equal(HttpStatusCode.Created, createDraft.StatusCode);
        var draftJson = await createDraft.Content.ReadFromJsonAsync<JsonElement>();
        var draftId = draftJson.GetProperty("id").GetGuid();

        Assert.Equal(34, draftJson.GetProperty("ecfType").GetInt32());
        Assert.Equal(EcfType.CreditNote34.ToString(), draftJson.GetProperty("ecfTypeName").GetString());
        Assert.False(draftJson.GetProperty("issued").GetBoolean());
        Assert.Equal(JsonValueKind.Null, draftJson.GetProperty("eNcf").ValueKind);
        Assert.False(draftJson.GetProperty("xmlGenerated").GetBoolean());
        Assert.False(draftJson.GetProperty("digitallySigned").GetBoolean());
        Assert.False(draftJson.GetProperty("submittedToDgii").GetBoolean());
        Assert.Equal(118m, draftJson.GetProperty("total").GetDecimal());

        // Natural idempotency: one fiscal draft per internal adjustment.
        var retryDraft = await client.PostAsync(
            $"/api/electronic-invoicing/drafts/credit-note/from-adjustment/{adjustmentId}",
            content: null);

        Assert.Equal(HttpStatusCode.OK, retryDraft.StatusCode);
        Assert.Equal(
            draftId,
            (await retryDraft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        var getDraft = await client.GetAsync($"/api/electronic-invoicing/drafts/{draftId}");
        Assert.Equal(HttpStatusCode.OK, getDraft.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            Assert.Equal(1, await db.AdjustmentDocuments.IgnoreQueryFilters().CountAsync());
            Assert.Equal(1, await db.ElectronicFiscalDocumentDrafts.IgnoreQueryFilters().CountAsync());

            var persistedDraft = await db.ElectronicFiscalDocumentDrafts.IgnoreQueryFilters().SingleAsync();
            Assert.Equal(EcfType.CreditNote34, persistedDraft.Type);
            Assert.Equal(adjustmentId, persistedDraft.AdjustmentDocumentId);
            Assert.Equal(returnId, persistedDraft.ReturnId);
        }
    }

    private static async Task ResetDatabaseAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }
}
