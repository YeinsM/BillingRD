using BillingRD.Application.Abstractions;
using BillingRD.Domain.Adjustments;
using BillingRD.Domain.Billing;
using BillingRD.Domain.Businesses;
using BillingRD.Domain.Catalog;
using BillingRD.Domain.Cash;
using BillingRD.Domain.Customers;
using BillingRD.Domain.ElectronicInvoicing;
using BillingRD.Domain.Identity;
using BillingRD.Domain.Inventory;
using BillingRD.Domain.Payments;
using BillingRD.Domain.Returns;
using BillingRD.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Infrastructure.Persistence;

public sealed class BillingDbContext(
    DbContextOptions<BillingDbContext> options,
    ICurrentBusinessContext currentBusinessContext) : DbContext(options)
{
    public Guid CurrentBusinessId => currentBusinessContext.BusinessId ?? Guid.Empty;

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<BusinessMembership> BusinessMemberships => Set<BusinessMembership>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<CashRegister> CashRegisters => Set<CashRegister>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<SalesReturn> Returns => Set<SalesReturn>();
    public DbSet<ReturnLine> ReturnLines => Set<ReturnLine>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<AdjustmentDocument> AdjustmentDocuments => Set<AdjustmentDocument>();
    public DbSet<ElectronicFiscalDocumentDraft> ElectronicFiscalDocumentDrafts => Set<ElectronicFiscalDocumentDraft>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);

        modelBuilder.Entity<Branch>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<BusinessMembership>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Product>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Customer>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Sale>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<SaleLine>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Invoice>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Payment>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<StockBalance>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<StockMovement>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<CashRegister>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<CashSession>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<CashMovement>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<SalesReturn>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<ReturnLine>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Refund>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<AdjustmentDocument>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<ElectronicFiscalDocumentDraft>().HasQueryFilter(entity => entity.BusinessId == CurrentBusinessId);
    }
}
