using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillingRD.Domain.Cash;
using BillingRD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BillingRD.Infrastructure.IntegrationTests;

public sealed class CashRegisterFlowTests
{
    [Fact]
    public async Task Cash_session_tracks_cash_sales_manual_expenses_retry_and_closing_difference()
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
                email = "cash-owner@example.test",
                password = "StrongPass123!"
            })).StatusCode);

        var businessResponse = await client.PostAsJsonAsync("/api/businesses/", new { name = "Caja Demo" });
        Assert.Equal(HttpStatusCode.Created, businessResponse.StatusCode);
        var businessJson = await businessResponse.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = businessJson.GetProperty("branchId").GetGuid();

        var registerResponse = await client.PostAsJsonAsync("/api/cash-registers/", new
        {
            branchId,
            name = "Caja Principal"
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registerId = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var openResponse = await client.PostAsJsonAsync("/api/cash-sessions/open", new
        {
            cashRegisterId = registerId,
            openingBalance = 100m
        });
        Assert.Equal(HttpStatusCode.Created, openResponse.StatusCode);
        var sessionId = (await openResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var duplicateOpen = await client.PostAsJsonAsync("/api/cash-sessions/open", new
        {
            cashRegisterId = registerId,
            openingBalance = 0m
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateOpen.StatusCode);

        var productResponse = await client.PostAsJsonAsync("/api/products/", new
        {
            name = "Servicio POS",
            sku = "CASH-SRV-01",
            salePrice = 100m,
            itbisCategory = "Standard",
            tracksInventory = false
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var sale = await SendSaleAsync(
            client,
            "cash-sale-001",
            branchId,
            productId,
            sessionId,
            new[]
            {
                new PaymentPayload("Cash", 50m, null),
                new PaymentPayload("Card", 68m, "AUTH-001")
            });
        Assert.Equal(HttpStatusCode.Created, sale.StatusCode);

        var sessionAfterSale = await client.GetFromJsonAsync<JsonElement>($"/api/cash-sessions/{sessionId}");
        Assert.Equal(150m, sessionAfterSale.GetProperty("expectedCash").GetDecimal());
        Assert.True(sessionAfterSale.GetProperty("isOpen").GetBoolean());

        // Retry must return the existing sale and must not duplicate the cash movement.
        var retry = await SendSaleAsync(
            client,
            "cash-sale-001",
            branchId,
            productId,
            sessionId,
            new[]
            {
                new PaymentPayload("Cash", 118m, null)
            });
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);

        var sessionAfterRetry = await client.GetFromJsonAsync<JsonElement>($"/api/cash-sessions/{sessionId}");
        Assert.Equal(150m, sessionAfterRetry.GetProperty("expectedCash").GetDecimal());

        var expense = await client.PostAsJsonAsync($"/api/cash-sessions/{sessionId}/movements", new
        {
            type = "Expense",
            amount = 20m,
            reason = "Compra de material de empaque"
        });
        Assert.Equal(HttpStatusCode.OK, expense.StatusCode);
        Assert.Equal(
            130m,
            (await expense.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("expectedCash").GetDecimal());

        var close = await client.PostAsJsonAsync($"/api/cash-sessions/{sessionId}/close", new
        {
            countedCash = 128m
        });
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        var closeJson = await close.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(130m, closeJson.GetProperty("expectedCash").GetDecimal());
        Assert.Equal(128m, closeJson.GetProperty("countedCash").GetDecimal());
        Assert.Equal(-2m, closeJson.GetProperty("difference").GetDecimal());

        var closedMovement = await client.PostAsJsonAsync($"/api/cash-sessions/{sessionId}/movements", new
        {
            type = "ManualIncome",
            amount = 1m,
            reason = "No permitido"
        });
        Assert.Equal(HttpStatusCode.Conflict, closedMovement.StatusCode);

        var cashSaleOnClosedSession = await SendSaleAsync(
            client,
            "cash-sale-002",
            branchId,
            productId,
            sessionId,
            new[]
            {
                new PaymentPayload("Cash", 118m, null)
            });
        Assert.Equal(HttpStatusCode.Conflict, cashSaleOnClosedSession.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        Assert.Equal(1, await db.CashRegisters.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await db.CashSessions.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.CashMovements.IgnoreQueryFilters().CountAsync());
        Assert.Equal(
            1,
            await db.CashMovements.IgnoreQueryFilters()
                .CountAsync(x => x.Type == CashMovementType.SaleCash));
        Assert.Equal(
            1,
            await db.CashMovements.IgnoreQueryFilters()
                .CountAsync(x => x.Type == CashMovementType.Expense));

        var persistedSession = await db.CashSessions.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(130m, persistedSession.ExpectedCashAtClose);
        Assert.Equal(128m, persistedSession.CountedCash);
        Assert.Equal(-2m, persistedSession.Difference);
    }

    [Fact]
    public async Task Card_only_sale_does_not_require_or_move_cash_session()
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
            email = "card-owner@example.test",
            password = "StrongPass123!"
        });

        var businessResponse = await client.PostAsJsonAsync("/api/businesses/", new { name = "Solo Tarjeta" });
        var branchId = (await businessResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("branchId").GetGuid();

        var productResponse = await client.PostAsJsonAsync("/api/products/", new
        {
            name = "Servicio Tarjeta",
            sku = "CARD-001",
            salePrice = 100m,
            itbisCategory = "Standard",
            tracksInventory = false
        });
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var sale = await SendSaleAsync(
            client,
            "card-sale-001",
            branchId,
            productId,
            null,
            new[] { new PaymentPayload("Card", 118m, "AUTH-CARD") });

        Assert.Equal(HttpStatusCode.Created, sale.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        Assert.Equal(0, await db.CashMovements.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Cash_payment_requires_an_open_cash_session()
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
            email = "missing-session@example.test",
            password = "StrongPass123!"
        });

        var businessResponse = await client.PostAsJsonAsync("/api/businesses/", new { name = "Sin Sesión" });
        var branchId = (await businessResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("branchId").GetGuid();

        var productResponse = await client.PostAsJsonAsync("/api/products/", new
        {
            name = "Servicio Cash",
            sku = "NOS-001",
            salePrice = 100m,
            itbisCategory = "Standard",
            tracksInventory = false
        });
        var productId = (await productResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        var sale = await SendSaleAsync(
            client,
            "missing-session-sale",
            branchId,
            productId,
            null,
            new[] { new PaymentPayload("Cash", 118m, null) });

        Assert.Equal(HttpStatusCode.BadRequest, sale.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        Assert.Equal(0, await db.Sales.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0, await db.CashMovements.IgnoreQueryFilters().CountAsync());
    }

    private static async Task<HttpResponseMessage> SendSaleAsync(
        HttpClient client,
        string idempotencyKey,
        Guid branchId,
        Guid productId,
        Guid? cashSessionId,
        IReadOnlyCollection<PaymentPayload> payments)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/sales/");
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Content = JsonContent.Create(new
        {
            branchId,
            customerId = (Guid?)null,
            items = new[] { new { productId, quantity = 1m } },
            payments,
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

    private sealed record PaymentPayload(string Method, decimal Amount, string? Reference);
}
