using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillingRD.Domain.ElectronicInvoicing;
using BillingRD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BillingRD.Infrastructure.IntegrationTests;

public sealed class FiscalInvoiceDraftTests
{
    [Fact]
    public async Task Ecf31_requires_fiscal_profile_and_identified_buyer_and_snapshots_both()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var branchId = await RegisterAndCreateBusinessAsync(client, "fiscal31@example.test", "Fiscal 31");

        var productId = await CreateProductAsync(client, "Servicio B2B", "B2B-001", 100m, "Standard");

        var customerResponse = await client.PostAsJsonAsync("/api/customers/", new
        {
            name = "Cliente Empresarial SRL",
            taxId = "101123456",
            fiscalAddress = "Av. Principal 10",
            email = "cliente@example.test"
        });
        Assert.Equal(HttpStatusCode.Created, customerResponse.StatusCode);
        var customerId = (await customerResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var sale = await CreateSaleAsync(client, "sale-31", branchId, productId, 1m, 118m, customerId);
        var invoiceId = sale.GetProperty("invoice").GetProperty("id").GetGuid();

        var withoutIssuer = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/invoice/{invoiceId}",
            new { type = "CreditFiscalInvoice31" });
        Assert.Equal(HttpStatusCode.Conflict, withoutIssuer.StatusCode);

        var profile = await client.PutAsJsonAsync("/api/fiscal-profile/", new
        {
            rnc = "101654321",
            legalName = "Comercio Fiscal SRL",
            tradeName = "Comercio Fiscal",
            address = "Calle Fiscal 1"
        });
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        var draftResponse = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/invoice/{invoiceId}",
            new { type = "CreditFiscalInvoice31" });
        Assert.Equal(HttpStatusCode.Created, draftResponse.StatusCode);

        var draft = await draftResponse.Content.ReadFromJsonAsync<JsonElement>();
        var draftId = draft.GetProperty("id").GetGuid();

        Assert.Equal(31, draft.GetProperty("ecfType").GetInt32());
        Assert.Equal("101654321", draft.GetProperty("issuer").GetProperty("rnc").GetString());
        Assert.Equal("Comercio Fiscal SRL", draft.GetProperty("issuer").GetProperty("legalName").GetString());
        Assert.Equal("101123456", draft.GetProperty("buyer").GetProperty("taxId").GetString());
        Assert.Equal("Cliente Empresarial SRL", draft.GetProperty("buyer").GetProperty("name").GetString());
        Assert.False(draft.GetProperty("issued").GetBoolean());
        Assert.Equal(JsonValueKind.Null, draft.GetProperty("eNcf").ValueKind);

        var retry = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/invoice/{invoiceId}",
            new { type = "CreditFiscalInvoice31" });
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(draftId, (await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        Assert.Equal(1, await db.BusinessFiscalProfiles.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await db.ElectronicFiscalDocumentDrafts.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Ecf32_allows_anonymous_below_threshold_but_requires_buyer_identity_at_250k()
    {
        await using var factory = await CreateFactoryAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var branchId = await RegisterAndCreateBusinessAsync(client, "fiscal32@example.test", "Fiscal 32");

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PutAsJsonAsync("/api/fiscal-profile/", new
            {
                rnc = "131123456",
                legalName = "Comercio Consumo SRL",
                tradeName = (string?)null,
                address = "Av. Consumo 20"
            })).StatusCode);

        var smallProductId = await CreateProductAsync(client, "Consumo pequeño", "C32-SMALL", 100m, "Standard");
        var smallSale = await CreateSaleAsync(client, "sale-32-small", branchId, smallProductId, 1m, 118m, null);
        var smallInvoiceId = smallSale.GetProperty("invoice").GetProperty("id").GetGuid();

        var smallDraft = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/invoice/{smallInvoiceId}",
            new { type = "ConsumerInvoice32" });
        Assert.Equal(HttpStatusCode.Created, smallDraft.StatusCode);
        var smallJson = await smallDraft.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(32, smallJson.GetProperty("ecfType").GetInt32());
        Assert.Equal(JsonValueKind.Null, smallJson.GetProperty("buyer").GetProperty("taxId").ValueKind);
        Assert.Equal(JsonValueKind.Null, smallJson.GetProperty("buyer").GetProperty("name").ValueKind);

        var largeProductId = await CreateProductAsync(client, "Consumo umbral", "C32-LARGE", 250000m, "ZeroRated");
        var largeAnonymousSale = await CreateSaleAsync(client, "sale-32-large-anon", branchId, largeProductId, 1m, 250000m, null);
        var largeAnonymousInvoiceId = largeAnonymousSale.GetProperty("invoice").GetProperty("id").GetGuid();

        var rejected = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/invoice/{largeAnonymousInvoiceId}",
            new { type = "ConsumerInvoice32" });
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);

        var foreignCustomerResponse = await client.PostAsJsonAsync("/api/customers/", new
        {
            name = "Foreign Buyer",
            foreignIdentifier = "PASSPORT-123",
            fiscalAddress = "International Address"
        });
        var foreignCustomerId = (await foreignCustomerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var largeIdentifiedSale = await CreateSaleAsync(
            client, "sale-32-large-identified", branchId, largeProductId, 1m, 250000m, foreignCustomerId);
        var largeIdentifiedInvoiceId = largeIdentifiedSale.GetProperty("invoice").GetProperty("id").GetGuid();

        var accepted = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/invoice/{largeIdentifiedInvoiceId}",
            new { type = "ConsumerInvoice32" });
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);

        var acceptedJson = await accepted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PASSPORT-123", acceptedJson.GetProperty("buyer").GetProperty("foreignIdentifier").GetString());
        Assert.Equal(250000m, acceptedJson.GetProperty("total").GetDecimal());
    }

    private static async Task<WebApplicationFactory<Program>> CreateFactoryAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("BILLINGRD_TEST_DB")
            ?? throw new InvalidOperationException("BILLINGRD_TEST_DB is required for integration tests.");

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:BillingDatabase", connectionString);
                builder.UseEnvironment("Testing");
            });

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        return factory;
    }

    private static async Task<Guid> RegisterAndCreateBusinessAsync(HttpClient client, string email, string businessName)
    {
        Assert.Equal(
            HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/auth/register", new { email, password = "StrongPass123!" })).StatusCode);

        var business = await client.PostAsJsonAsync("/api/businesses/", new { name = businessName });
        Assert.Equal(HttpStatusCode.Created, business.StatusCode);
        return (await business.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("branchId").GetGuid();
    }

    private static async Task<Guid> CreateProductAsync(
        HttpClient client,
        string name,
        string sku,
        decimal price,
        string itbisCategory)
    {
        var response = await client.PostAsJsonAsync("/api/products/", new
        {
            name,
            sku,
            salePrice = price,
            itbisCategory,
            tracksInventory = false
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> CreateSaleAsync(
        HttpClient client,
        string idempotencyKey,
        Guid branchId,
        Guid productId,
        decimal quantity,
        decimal total,
        Guid? customerId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            branchId,
            customerId,
            items = new[] { new { productId, quantity } },
            payments = new[] { new { method = "Card", amount = total, reference = "FISCAL-TEST" } }
        });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
