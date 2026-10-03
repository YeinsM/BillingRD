using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BillingRD.Infrastructure;

/// <summary>
/// Registers PostgreSQL persistence while keeping provider details outside the API layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<BillingDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
