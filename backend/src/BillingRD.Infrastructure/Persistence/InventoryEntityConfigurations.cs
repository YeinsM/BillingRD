using BillingRD.Domain.Businesses;
using BillingRD.Domain.Catalog;
using BillingRD.Domain.Identity;
using BillingRD.Domain.Inventory;
using BillingRD.Domain.Sales;
using BillingRD.Domain.Returns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingRD.Infrastructure.Persistence;

internal sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("stock_balances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.ProductId }).IsUnique();
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.QuantityDelta).HasPrecision(18, 3);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 3);
        builder.Property(x => x.Reason).HasMaxLength(240);

        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesReturn>().WithMany().HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SaleId, x.ProductId })
            .IsUnique()
            .HasFilter(@"""SaleId"" IS NOT NULL AND ""ReturnId"" IS NULL");
        builder.HasIndex(x => new { x.ReturnId, x.ProductId })
            .IsUnique()
            .HasFilter(@"""ReturnId"" IS NOT NULL");
        builder.HasIndex(x => new { x.BranchId, x.ProductId, x.CreatedAtUtc });
    }
}
