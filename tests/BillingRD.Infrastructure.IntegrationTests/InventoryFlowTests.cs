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

public sealed class InventoryFlowTests
{
    [Fact]
    public async Task Tracked_stock_is_adjusted_consumed_and_not_duplicated_by_sale_retries()
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
                email = "inventory-owner@example.test",
                password = "StrongPass123!"
            })).StatusCode);

        var businessResponse = await client.PostAsJsonAsync("/api/businesses/", new { name = "Tienda Inventario" });
        Assert.Equal(HttpStatusCode.Created, businessResponse.StatusCode);
        var businessJson = await businessResponse.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = businessJson.GetProperty("branchId").GetGuid();

        var productResponse = await client.PostAsJsonAsync("/api/products/", new
        {
            name = "Producto controlado",
            sku = "INV-001",
            salePrice = 100m,
            itbisCategory = "Standard",
            tracksInventory = true
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();

        var initialInventory = await client.GetFromJsonAsync<JsonElement>($"/api/inventory/?branchId={branchId}");
        Assert.Single(initialInventory.EnumerateArray());
        Assert.Equal(0m, initialInventory[0].GetProperty("quantity").GetDecimal());

        var adjustment = await client.PostAsJsonAsync("/api/inventory/adjustments", new
        {
            branchId,
            productId,
            quantityDelta = 5m,
            reason = "Inventario inicial"
        });
        Assert.Equal(HttpStatusCode.OK, adjustment.StatusCode);

        var afterAdjustment = await adjustment.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5m, afterAdjustment.GetProperty("quantity").GetDecimal());

        var saleResponse = await SendSaleAsync(client, "inventory-sale-001", branchId, productId, 2m, 236m);
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);

        var inventoryAfterSale = await client.GetFromJsonAsync<JsonElement>($"/api/inventory/?branchId={branchId}");
        Assert.Equal(3m, inventoryAfterSale[0].GetProperty("quantity").GetDecimal());

        var movements = await client.GetFromJsonAsync<JsonElement>(
            $"/api/inventory/movements?branchId={branchId}&productId={productId}");
        Assert.Equal(2, movements.GetArrayLength());
        Assert.Contains(movements.EnumerateArray(), item =>
            item.GetProperty("type").GetString() == "Initial" &&
            item.GetProperty("quantityDelta").GetDecimal() == 5m);
        Assert.Contains(movements.EnumerateArray(), item =>
            item.GetProperty("type").GetString() == "Sale" &&
            item.GetProperty("quantityDelta").GetDecimal() == -2m &&
            item.GetProperty("balanceAfter").GetDecimal() == 3m);

        // Same idempotency key returns the existing sale and must not consume stock again.
        var retry = await SendSaleAsync(client, "inventory-sale-001", branchId, productId, 3m, 354m);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);

        var inventoryAfterRetry = await client.GetFromJsonAsync<JsonElement>($"/api/inventory/?branchId={branchId}");
        Assert.Equal(3m, inventoryAfterRetry[0].GetProperty("quantity").GetDecimal());

        // A new sale cannot push stock below zero.
        var insufficient = await SendSaleAsync(client, "inventory-sale-002", branchId, productId, 4m, 472m);
        Assert.Equal(HttpStatusCode.Conflict, insufficient.StatusCode);

        var negativeAdjustment = await client.PostAsJsonAsync("/api/inventory/adjustments", new
        {
            branchId,
            productId,
            quantityDelta = -4m,
            reason = "Intento inválido"
        });
        Assert.Equal(HttpStatusCode.Conflict, negativeAdjustment.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        Assert.Equal(1, await db.Sales.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await db.StockBalances.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.StockMovements.IgnoreQueryFilters().CountAsync());
        Assert.Equal(3m, await db.StockBalances.IgnoreQueryFilters().Select(x => x.Quantity).SingleAsync());
    }

    [Fact]
    public async Task Non_inventory_product_can_be_sold_without_stock_balance()
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

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "service-owner@example.test",
            password = "StrongPass123!"
        });

        var businessResponse = await client.PostAsJsonAsync("/api/businesses/", new { name = "Servicios RD" });
        var branchId = (await businessResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("branchId")
            .GetGuid();

        var productResponse = await client.PostAsJsonAsync("/api/products/", new
        {
            name = "Servicio técnico",
            sku = "SRV-001",
            salePrice = 100m,
            itbisCategory = "Standard",
            tracksInventory = false
        });
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();

        var sale = await SendSaleAsync(client, "service-sale-001", branchId, productId, 1m, 118m);
        Assert.Equal(HttpStatusCode.Created, sale.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        Assert.Equal(0, await db.StockBalances.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0, await db.StockMovements.IgnoreQueryFilters().CountAsync());
    }


    [Fact]
    public async Task Concurrent_sales_cannot_both_consume_the_last_unit()
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

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "concurrent-stock@example.test",
            password = "StrongPass123!"
        });

        var businessResponse = await client.PostAsJsonAsync("/api/businesses/", new { name = "Stock Concurrente" });
        var branchId = (await businessResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("branchId")
            .GetGuid();

        var productResponse = await client.PostAsJsonAsync("/api/products/", new
        {
            name = "Última unidad",
            sku = "LAST-001",
            salePrice = 100m,
            itbisCategory = "Standard",
            tracksInventory = true
        });
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/inventory/adjustments", new
            {
                branchId,
                productId,
                quantityDelta = 1m,
                reason = "Última unidad"
            })).StatusCode);

        var firstTask = SendSaleAsync(client, "concurrent-001", branchId, productId, 1m, 118m);
        var secondTask = SendSaleAsync(client, "concurrent-002", branchId, productId, 1m, 118m);

        var responses = await Task.WhenAll(firstTask, secondTask);
        Assert.Single(responses.Where(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Single(responses.Where(response => response.StatusCode == HttpStatusCode.Conflict));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        Assert.Equal(1, await db.Sales.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0m, await db.StockBalances.IgnoreQueryFilters().Select(x => x.Quantity).SingleAsync());
        Assert.Equal(
            1,
            await db.StockMovements.IgnoreQueryFilters()
                .CountAsync(movement => movement.SaleId != null));
    }

    private static async Task<HttpResponseMessage> SendSaleAsync(
        HttpClient client,
        string idempotencyKey,
        Guid branchId,
        Guid productId,
        decimal quantity,
        decimal paymentAmount)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            branchId,
            customerId = (Guid?)null,
            items = new[] { new { productId, quantity } },
            payments = new[] { new { method = "Cash", amount = paymentAmount, reference = (string?)null } }
        });

        return await client.SendAsync(request);
    }

    private static async Task ResetDatabaseAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }
}
