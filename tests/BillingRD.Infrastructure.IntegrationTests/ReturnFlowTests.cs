using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillingRD.Domain.Cash;
using BillingRD.Domain.Inventory;
using BillingRD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BillingRD.Infrastructure.IntegrationTests;

public sealed class ReturnFlowTests
{
    [Fact]
    public async Task Partial_returns_restore_stock_refund_original_tenders_and_are_idempotent()
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
                email = "return-owner@example.test",
                password = "StrongPass123!"
            })).StatusCode);

        var businessResponse = await client.PostAsJsonAsync("/api/businesses/", new { name = "Tienda Devoluciones" });
        Assert.Equal(HttpStatusCode.Created, businessResponse.StatusCode);
        var businessJson = await businessResponse.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = businessJson.GetProperty("branchId").GetGuid();

        var registerResponse = await client.PostAsJsonAsync("/api/cash-registers/", new
        {
            branchId,
            name = "Caja Devoluciones"
        });
        var registerId = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var openResponse = await client.PostAsJsonAsync("/api/cash-sessions/open", new
        {
            cashRegisterId = registerId,
            openingBalance = 200m
        });
        var cashSessionId = (await openResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var productResponse = await client.PostAsJsonAsync("/api/products/", new
        {
            name = "Producto retornable",
            sku = "RET-001",
            salePrice = 100m,
            itbisCategory = "Standard",
            tracksInventory = true
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/inventory/adjustments", new
            {
                branchId,
                productId,
                quantityDelta = 5m,
                reason = "Inventario inicial"
            })).StatusCode);

        var saleResponse = await SendSaleAsync(client, branchId, productId, cashSessionId);
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);
        var saleJson = await saleResponse.Content.ReadFromJsonAsync<JsonElement>();
        var saleId = saleJson.GetProperty("id").GetGuid();
        Assert.Equal(236m, saleJson.GetProperty("total").GetDecimal());

        var stockAfterSale = await client.GetFromJsonAsync<JsonElement>($"/api/inventory/?branchId={branchId}");
        Assert.Equal(3m, stockAfterSale[0].GetProperty("quantity").GetDecimal());

        var firstReturn = await SendReturnAsync(
            client,
            "return-001",
            saleId,
            productId,
            1m,
            cashSessionId,
            cashAmount: 50m,
            cardAmount: 68m);

        Assert.Equal(HttpStatusCode.Created, firstReturn.StatusCode);
        var firstReturnJson = await firstReturn.Content.ReadFromJsonAsync<JsonElement>();
        var firstReturnId = firstReturnJson.GetProperty("id").GetGuid();
        Assert.Equal(100m, firstReturnJson.GetProperty("subtotal").GetDecimal());
        Assert.Equal(18m, firstReturnJson.GetProperty("taxAmount").GetDecimal());
        Assert.Equal(118m, firstReturnJson.GetProperty("total").GetDecimal());

        var stockAfterFirstReturn = await client.GetFromJsonAsync<JsonElement>($"/api/inventory/?branchId={branchId}");
        Assert.Equal(4m, stockAfterFirstReturn[0].GetProperty("quantity").GetDecimal());

        var sessionAfterFirstReturn = await client.GetFromJsonAsync<JsonElement>($"/api/cash-sessions/{cashSessionId}");
        Assert.Equal(250m, sessionAfterFirstReturn.GetProperty("expectedCash").GetDecimal());

        // Same idempotency key must return the original return without a second stock/cash reversal.
        var retry = await SendReturnAsync(
            client,
            "return-001",
            saleId,
            productId,
            1m,
            cashSessionId,
            cashAmount: 50m,
            cardAmount: 68m);

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(
            firstReturnId,
            (await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        var stockAfterRetry = await client.GetFromJsonAsync<JsonElement>($"/api/inventory/?branchId={branchId}");
        Assert.Equal(4m, stockAfterRetry[0].GetProperty("quantity").GetDecimal());

        // Only RD$50 cash remains refundable because the original sale had RD$100 cash and RD$50 was already refunded.
        var invalidTenderRefund = await SendReturnAsync(
            client,
            "return-invalid-tender",
            saleId,
            productId,
            1m,
            cashSessionId,
            cashAmount: 118m,
            cardAmount: 0m);

        Assert.Equal(HttpStatusCode.Conflict, invalidTenderRefund.StatusCode);

        var secondReturn = await SendReturnAsync(
            client,
            "return-002",
            saleId,
            productId,
            1m,
            cashSessionId,
            cashAmount: 50m,
            cardAmount: 68m);

        Assert.Equal(HttpStatusCode.Created, secondReturn.StatusCode);

        var stockAfterSecondReturn = await client.GetFromJsonAsync<JsonElement>($"/api/inventory/?branchId={branchId}");
        Assert.Equal(5m, stockAfterSecondReturn[0].GetProperty("quantity").GetDecimal());

        var sessionAfterSecondReturn = await client.GetFromJsonAsync<JsonElement>($"/api/cash-sessions/{cashSessionId}");
        Assert.Equal(200m, sessionAfterSecondReturn.GetProperty("expectedCash").GetDecimal());

        var overReturn = await SendReturnAsync(
            client,
            "return-003",
            saleId,
            productId,
            1m,
            cashSessionId,
            cashAmount: 50m,
            cardAmount: 68m);

        Assert.Equal(HttpStatusCode.Conflict, overReturn.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        Assert.Equal(2, await db.Returns.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.ReturnLines.IgnoreQueryFilters().CountAsync());
        Assert.Equal(4, await db.Refunds.IgnoreQueryFilters().CountAsync());
        Assert.Equal(
            2,
            await db.StockMovements.IgnoreQueryFilters()
                .CountAsync(x => x.Type == StockMovementType.Return));
        Assert.Equal(
            2,
            await db.CashMovements.IgnoreQueryFilters()
                .CountAsync(x => x.Type == CashMovementType.RefundCash));
    }

    private static async Task<HttpResponseMessage> SendSaleAsync(
        HttpClient client,
        Guid branchId,
        Guid productId,
        Guid cashSessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/");
        request.Headers.Add("Idempotency-Key", "return-source-sale");
        request.Content = JsonContent.Create(new
        {
            branchId,
            customerId = (Guid?)null,
            items = new[] { new { productId, quantity = 2m } },
            payments = new object[]
            {
                new { method = "Cash", amount = 100m, reference = (string?)null },
                new { method = "Card", amount = 136m, reference = "AUTH-RETURN" }
            },
            cashSessionId
        });

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendReturnAsync(
        HttpClient client,
        string idempotencyKey,
        Guid saleId,
        Guid productId,
        decimal quantity,
        Guid cashSessionId,
        decimal cashAmount,
        decimal cardAmount)
    {
        var refunds = new List<object>();
        if (cashAmount > 0)
            refunds.Add(new { method = "Cash", amount = cashAmount, reference = (string?)null });
        if (cardAmount > 0)
            refunds.Add(new { method = "Card", amount = cardAmount, reference = "REFUND-CARD" });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/returns/");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            saleId,
            reason = "Cliente devolvió el producto",
            items = new[] { new { productId, quantity } },
            refunds,
            cashSessionId
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
