using Xunit;
using BillingRD.Application.Abstractions;
using BillingRD.Domain.Businesses;
using BillingRD.Domain.Catalog;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Infrastructure.IntegrationTests;

public sealed class BusinessIsolationTests
{
    [Fact]
    public async Task PostgreSql_constraints_and_business_filters_protect_scoped_data()
    {
        var connectionString = Environment.GetEnvironmentVariable("BILLINGRD_TEST_DB")
            ?? throw new InvalidOperationException("BILLINGRD_TEST_DB is required for integration tests.");

        await using (var setup = CreateContext(connectionString, null))
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.MigrateAsync();

            var businessA = Business.Create("Tienda A");
            var businessB = Business.Create("Tienda B");

            setup.Businesses.AddRange(businessA, businessB);
            setup.Products.AddRange(
                Product.Create(businessA.Id, "Café", "SKU-001", 150m),
                Product.Create(businessB.Id, "Café", "SKU-001", 175m));

            await setup.SaveChangesAsync();
        }

        var businessIds = await GetBusinessIdsAsync(connectionString);

        await using (var businessAContext = CreateContext(connectionString, businessIds[0]))
        {
            var products = await businessAContext.Products.ToListAsync();

            Assert.Single(products);
            Assert.Equal(businessIds[0], products[0].BusinessId);

            // The unique SKU constraint is scoped by BusinessId, so duplicates inside one business are rejected.
            businessAContext.Products.Add(Product.Create(businessIds[0], "Otro café", "SKU-001", 200m));
            await Assert.ThrowsAsync<DbUpdateException>(() => businessAContext.SaveChangesAsync());
        }

        await using (var businessBContext = CreateContext(connectionString, businessIds[1]))
        {
            var products = await businessBContext.Products.ToListAsync();

            Assert.Single(products);
            Assert.Equal(businessIds[1], products[0].BusinessId);
        }

        await using (var unresolvedContext = CreateContext(connectionString, null))
        {
            // Fail closed: without a trusted business context, scoped data is invisible.
            Assert.Empty(await unresolvedContext.Products.ToListAsync());
        }
    }

    private static async Task<Guid[]> GetBusinessIdsAsync(string connectionString)
    {
        await using var context = CreateContext(connectionString, null);
        return await context.Businesses
            .OrderBy(x => x.Name)
            .Select(x => x.Id)
            .ToArrayAsync();
    }

    private static BillingDbContext CreateContext(string connectionString, Guid? businessId)
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BillingDbContext(options, new TestBusinessContext(businessId));
    }

    private sealed class TestBusinessContext(Guid? businessId) : ICurrentBusinessContext
    {
        public Guid? BusinessId { get; } = businessId;
    }
}
