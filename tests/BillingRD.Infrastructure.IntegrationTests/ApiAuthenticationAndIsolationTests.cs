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

public sealed class ApiAuthenticationAndIsolationTests
{
    [Fact]
    public async Task Authenticated_users_can_only_operate_inside_verified_business_memberships()
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

        var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/products/")).StatusCode);

        var clientA = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var registrationA = await clientA.PostAsJsonAsync("/api/auth/register", new
        {
            email = "owner-a@example.test",
            password = "StrongPass123!"
        });
        Assert.Equal(HttpStatusCode.Created, registrationA.StatusCode);

        // A registered user has no active business until one is created or selected.
        Assert.Equal(HttpStatusCode.Conflict, (await clientA.GetAsync("/api/products/")).StatusCode);

        var businessAResponse = await clientA.PostAsJsonAsync("/api/businesses/", new { name = "Tienda A" });
        Assert.Equal(HttpStatusCode.Created, businessAResponse.StatusCode);
        var businessAId = await ReadGuidAsync(businessAResponse, "id");

        Assert.Equal(
            HttpStatusCode.Created,
            (await clientA.PostAsJsonAsync("/api/products/", new
            {
                name = "Café A",
                sku = "SKU-001",
                salePrice = 150m
            })).StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            (await clientA.PostAsJsonAsync("/api/customers/", new { name = "Cliente A" })).StatusCode);

        var clientB = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var registrationB = await clientB.PostAsJsonAsync("/api/auth/register", new
        {
            email = "owner-b@example.test",
            password = "StrongPass123!"
        });
        Assert.Equal(HttpStatusCode.Created, registrationB.StatusCode);

        var businessBResponse = await clientB.PostAsJsonAsync("/api/businesses/", new { name = "Tienda B" });
        Assert.Equal(HttpStatusCode.Created, businessBResponse.StatusCode);
        var businessBId = await ReadGuidAsync(businessBResponse, "id");

        Assert.Equal(
            HttpStatusCode.Created,
            (await clientB.PostAsJsonAsync("/api/products/", new
            {
                name = "Café B",
                sku = "SKU-001",
                salePrice = 175m
            })).StatusCode);

        // User A cannot activate user B's business even when the exact ID is known.
        var forbiddenSwitch = await clientA.PostAsJsonAsync(
            "/api/auth/select-business",
            new { businessId = businessBId });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenSwitch.StatusCode);

        var productsA = await clientA.GetFromJsonAsync<JsonElement>("/api/products/");
        Assert.Equal(1, productsA.GetArrayLength());
        Assert.Equal("Café A", productsA[0].GetProperty("name").GetString());

        var productsB = await clientB.GetFromJsonAsync<JsonElement>("/api/products/");
        Assert.Equal(1, productsB.GetArrayLength());
        Assert.Equal("Café B", productsB[0].GetProperty("name").GetString());

        Assert.NotEqual(businessAId, businessBId);
    }

    private static async Task ResetDatabaseAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response, string propertyName)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(propertyName).GetGuid();
    }
}
