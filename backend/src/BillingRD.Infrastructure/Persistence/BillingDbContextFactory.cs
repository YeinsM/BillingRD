using BillingRD.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BillingRD.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by EF tooling. Runtime configuration continues to come from dependency injection.
/// </summary>
public sealed class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("BILLINGRD_DESIGN_DB")
            ?? "Host=localhost;Port=5432;Database=billingrd;Username=billingrd;Password=billingrd_dev_only";

        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BillingDbContext(options, new DesignTimeBusinessContext());
    }

    private sealed class DesignTimeBusinessContext : ICurrentBusinessContext
    {
        public Guid? BusinessId => null;
    }
}
