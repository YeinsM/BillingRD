using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BillingRD.Infrastructure;

/// <summary>
/// Registers infrastructure concerns without leaking PostgreSQL details into the API project.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BillingDatabase")
            ?? throw new InvalidOperationException("Connection string 'BillingDatabase' is required.");

        services.AddDbContext<BillingDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
