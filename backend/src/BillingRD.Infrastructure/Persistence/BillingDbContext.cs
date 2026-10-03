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
    public Guid? CurrentBusinessId => currentBusinessContext.BusinessId;

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<BusinessMembership> BusinessMemberships => Set<BusinessMembership>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

        // Fail closed: without a trusted business context, scoped queries return no rows.
        modelBuilder.Entity<Branch>()
            .HasQueryFilter(branch => CurrentBusinessId.HasValue && branch.BusinessId == CurrentBusinessId.Value);
        modelBuilder.Entity<BusinessMembership>()
            .HasQueryFilter(membership => CurrentBusinessId.HasValue && membership.BusinessId == CurrentBusinessId.Value);
        modelBuilder.Entity<Product>()
            .HasQueryFilter(product => CurrentBusinessId.HasValue && product.BusinessId == CurrentBusinessId.Value);
        modelBuilder.Entity<Customer>()
            .HasQueryFilter(customer => CurrentBusinessId.HasValue && customer.BusinessId == CurrentBusinessId.Value);
    }
}
