using BillingRD.Domain.Businesses;
using BillingRD.Domain.Cash;
using BillingRD.Domain.Identity;
using BillingRD.Domain.Payments;
using BillingRD.Domain.Sales;
using BillingRD.Domain.Returns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingRD.Infrastructure.Persistence;

internal sealed class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("cash_registers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.BranchId, x.Name }).IsUnique();
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CashSessionConfiguration : IEntityTypeConfiguration<CashSession>
{
    public void Configure(EntityTypeBuilder<CashSession> builder)
    {
        builder.ToTable("cash_sessions");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.IsOpen);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.ExpectedCashAtClose).HasPrecision(18, 2);
        builder.Property(x => x.CountedCash).HasPrecision(18, 2);
        builder.Property(x => x.Difference).HasPrecision(18, 2);

        builder.HasIndex(x => x.CashRegisterId)
            .IsUnique()
            .HasFilter(@"""ClosedAtUtc"" IS NULL");

        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashRegister>().WithMany().HasForeignKey(x => x.CashRegisterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.OpenedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("cash_movements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Reason).HasMaxLength(240);

        builder.HasIndex(x => x.PaymentId)
            .IsUnique()
            .HasFilter(@"""PaymentId"" IS NOT NULL");
        builder.HasIndex(x => x.RefundId)
            .IsUnique()
            .HasFilter(@"""RefundId"" IS NOT NULL");

        builder.HasIndex(x => new { x.CashSessionId, x.CreatedAtUtc });

        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Refund>().WithMany().HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.Restrict);
    }
}
