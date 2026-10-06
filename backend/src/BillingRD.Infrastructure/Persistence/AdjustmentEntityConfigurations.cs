using BillingRD.Domain.Adjustments;
using BillingRD.Domain.Businesses;
using BillingRD.Domain.ElectronicInvoicing;
using BillingRD.Domain.Identity;
using BillingRD.Domain.Returns;
using BillingRD.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingRD.Infrastructure.Persistence;

internal sealed class AdjustmentDocumentConfiguration : IEntityTypeConfiguration<AdjustmentDocument>
{
    public void Configure(EntityTypeBuilder<AdjustmentDocument> builder)
    {
        builder.ToTable("adjustment_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Reason).HasMaxLength(240).IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);

        builder.HasIndex(x => x.ReturnId).IsUnique();
        builder.HasIndex(x => x.SaleId);

        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesReturn>().WithMany().HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ElectronicFiscalDocumentDraftConfiguration : IEntityTypeConfiguration<ElectronicFiscalDocumentDraft>
{
    public void Configure(EntityTypeBuilder<ElectronicFiscalDocumentDraft> builder)
    {
        builder.ToTable("electronic_fiscal_document_drafts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);

        builder.HasIndex(x => x.AdjustmentDocumentId).IsUnique();
        builder.HasIndex(x => x.ReturnId).IsUnique();
        builder.HasIndex(x => x.SaleId);

        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdjustmentDocument>().WithMany().HasForeignKey(x => x.AdjustmentDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesReturn>().WithMany().HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
