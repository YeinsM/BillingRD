using BillingRD.Application.Abstractions;
using BillingRD.Domain.Businesses;
using BillingRD.Domain.Catalog;
using BillingRD.Domain.Customers;
using BillingRD.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL unit of work for BillingRD. Business-scoped entities are filtered by the trusted current business context.
/// </summary>
public sealed class BillingDbContext(
    DbContextOptions<BillingDbContext> options,
    ICurrentBusinessContext currentBusinessContext) : DbContext(options)
{
    // Guid.Empty is never a valid Business id in the domain, so unresolved contexts safely match zero scoped rows.
    public Guid CurrentBusinessId => currentBusinessContext.BusinessId ?? Guid.Empty;

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<BusinessMembership> BusinessMemberships => Set<BusinessMembership>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

        // Fail closed: without a trusted business context, CurrentBusinessId is Guid.Empty and no scoped row can match.
        modelBuilder.Entity<Branch>().HasQueryFilter(branch => branch.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<BusinessMembership>().HasQueryFilter(membership => membership.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Product>().HasQueryFilter(product => product.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Customer>().HasQueryFilter(customer => customer.BusinessId == CurrentBusinessId);
    }
}
