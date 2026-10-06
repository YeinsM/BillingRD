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

public sealed class SaleFlowTests
{
    [Fact]
    public async Task Sale_is_atomic_taxed_paid_and_idempotent_inside_the_active_business()
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
                email = "cashier-owner@example.test",
                password = "StrongPass123!"
            })).StatusCode);

        var businessResponse = await client.PostAsJsonAsync("/api/businesses/", new { name = "Tienda Fiscal" });
        Assert.Equal(HttpStatusCode.Created, businessResponse.StatusCode);

        var businessJson = await businessResponse.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = businessJson.GetProperty("branchId").GetGuid();

        var standardId = await CreateProductAsync(client, "Producto 18", "STD-18", 100m, "Standard");
        var reducedId = await CreateProductAsync(client, "Producto 16", "RED-16", 100m, "Reduced");
        var exemptId = await CreateProductAsync(client, "Producto Exento", "EX-00", 50m, "Exempt");
        var zeroRatedId = await CreateProductAsync(client, "Producto 0%", "ZR-00", 25m, "ZeroRated");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/");
        request.Headers.Add("Idempotency-Key", "sale-test-001");
        request.Content = JsonContent.Create(new
        {
            branchId,
            customerId = (Guid?)null,
            items = new[]
            {
                new { productId = standardId, quantity = 1m },
                new { productId = reducedId, quantity = 1m },
                new { productId = exemptId, quantity = 1m },
                new { productId = zeroRatedId, quantity = 1m }
            },
            payments = new object[]
            {
                new { method = "Card", amount = 200m, reference = "AUTH-001" },
                new { method = "BankTransfer", amount = 109m, reference = "TRX-001" }
            }
        });

        var saleResponse = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);

        var saleJson = await saleResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(275m, saleJson.GetProperty("subtotal").GetDecimal());
        Assert.Equal(34m, saleJson.GetProperty("taxAmount").GetDecimal());
        Assert.Equal(309m, saleJson.GetProperty("total").GetDecimal());
        Assert.Equal(4, saleJson.GetProperty("lines").GetArrayLength());

        var categories = saleJson.GetProperty("lines")
            .EnumerateArray()
            .Select(line => line.GetProperty("itbisCategory").GetString())
            .ToHashSet();
        Assert.Contains("Exempt", categories);
        Assert.Contains("ZeroRated", categories);
        Assert.Equal(2, saleJson.GetProperty("payments").GetArrayLength());

        var saleId = saleJson.GetProperty("id").GetGuid();
        var invoiceId = saleJson.GetProperty("invoice").GetProperty("id").GetGuid();

        using var retry = new HttpRequestMessage(HttpMethod.Post, "/api/sales/");
        retry.Headers.Add("Idempotency-Key", "sale-test-001");
        retry.Content = JsonContent.Create(new
        {
            branchId,
            customerId = (Guid?)null,
            items = new[] { new { productId = standardId, quantity = 99m } },
            payments = new[] { new { method = "Card", amount = 1m, reference = "RETRY" } }
        });

        var retryResponse = await client.SendAsync(retry);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        var retryJson = await retryResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(saleId, retryJson.GetProperty("id").GetGuid());
        Assert.Equal(invoiceId, retryJson.GetProperty("invoice").GetProperty("id").GetGuid());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        Assert.Equal(1, await db.Sales.IgnoreQueryFilters().CountAsync());
        Assert.Equal(4, await db.SaleLines.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await db.Invoices.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.Payments.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Sale_rejects_wrong_payment_total_and_products_from_another_business()
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

        var clientA = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await RegisterAsync(clientA, "owner-a-sale@example.test");
        var businessA = await CreateBusinessAsync(clientA, "Negocio A");
        var productA = await CreateProductAsync(clientA, "A", "A-1", 100m, "Standard");

        var wrongPayment = await SendSaleAsync(
            clientA,
            "wrong-payment",
            businessA.branchId,
            productA,
            100m);

        Assert.Equal(HttpStatusCode.BadRequest, wrongPayment.StatusCode);

        var clientB = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await RegisterAsync(clientB, "owner-b-sale@example.test");
        var businessB = await CreateBusinessAsync(clientB, "Negocio B");
        var productB = await CreateProductAsync(clientB, "B", "B-1", 100m, "Standard");

        var foreignProduct = await SendSaleAsync(
            clientA,
            "foreign-product",
            businessA.branchId,
            productB,
            118m);

        Assert.Equal(HttpStatusCode.BadRequest, foreignProduct.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        Assert.Equal(0, await db.Sales.IgnoreQueryFilters().CountAsync());

        Assert.NotEqual(businessA.businessId, businessB.businessId);
    }

    private static async Task<HttpResponseMessage> SendSaleAsync(
        HttpClient client,
        string idempotencyKey,
        Guid branchId,
        Guid productId,
        decimal paymentAmount)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            branchId,
            customerId = (Guid?)null,
            items = new[] { new { productId, quantity = 1m } },
            payments = new[] { new { method = "Card", amount = paymentAmount, reference = "SALE-TEST" } }
        });

        return await client.SendAsync(request);
    }

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "StrongPass123!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<(Guid businessId, Guid branchId)> CreateBusinessAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/businesses/", new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("id").GetGuid(), json.GetProperty("branchId").GetGuid());
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

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetGuid();
    }

    private static async Task ResetDatabaseAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }
}
