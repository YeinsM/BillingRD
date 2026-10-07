using BillingRD.Domain.Adjustments;
using BillingRD.Domain.Businesses;
using BillingRD.Domain.Billing;
using BillingRD.Domain.Customers;
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
        builder.Property(x => x.IssuerRnc).HasMaxLength(11);
        builder.Property(x => x.IssuerLegalName).HasMaxLength(150);
        builder.Property(x => x.IssuerTradeName).HasMaxLength(150);
        builder.Property(x => x.IssuerAddress).HasMaxLength(100);
        builder.Property(x => x.BuyerTaxId).HasMaxLength(11);
        builder.Property(x => x.BuyerForeignIdentifier).HasMaxLength(20);
        builder.Property(x => x.BuyerName).HasMaxLength(150);
        builder.Property(x => x.BuyerAddress).HasMaxLength(100);
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);

        builder.HasIndex(x => x.InvoiceId).IsUnique().HasFilter(@"""InvoiceId"" IS NOT NULL");
        builder.HasIndex(x => x.AdjustmentDocumentId).IsUnique().HasFilter(@"""AdjustmentDocumentId"" IS NOT NULL");
        builder.HasIndex(x => x.ReturnId).IsUnique().HasFilter(@"""ReturnId"" IS NOT NULL");
        builder.HasIndex(x => x.SaleId);

        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdjustmentDocument>().WithMany().HasForeignKey(x => x.AdjustmentDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesReturn>().WithMany().HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
