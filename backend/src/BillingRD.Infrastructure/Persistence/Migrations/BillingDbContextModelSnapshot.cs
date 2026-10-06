using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BillingRD.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
partial class BillingDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("BillingRD.Domain.Billing.Invoice", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<Guid?>("CustomerId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("IssuedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("SaleId").HasColumnType("uuid");
            b.Property<decimal>("Subtotal").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("TaxAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("Total").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.HasKey("Id");
            b.HasIndex("BusinessId");
            b.HasIndex("CustomerId");
            b.HasIndex("SaleId").IsUnique();
            b.ToTable("invoices");
        });

        modelBuilder.Entity("BillingRD.Domain.Businesses.Business", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("LegalName").HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("Name").IsRequired().HasMaxLength(160).HasColumnType("character varying(160)");
            b.Property<string>("TaxId").HasMaxLength(20).HasColumnType("character varying(20)");
            b.HasKey("Id");
            b.HasIndex("TaxId").IsUnique().HasFilter(@"""TaxId"" IS NOT NULL");
            b.ToTable("businesses");
        });

        modelBuilder.Entity("BillingRD.Domain.Businesses.Branch", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.HasKey("Id");
            b.HasIndex("BusinessId", "Name").IsUnique();
            b.ToTable("branches");
        });

        modelBuilder.Entity("BillingRD.Domain.Catalog.Product", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<string>("Barcode").HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("ItbisCategory").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)");
            b.Property<bool>("TracksInventory").HasColumnType("boolean").HasDefaultValue(true);
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<decimal>("SalePrice").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<string>("Sku").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.HasKey("Id");
            b.HasIndex("BusinessId", "Barcode").IsUnique().HasFilter(@"""Barcode"" IS NOT NULL");
            b.HasIndex("BusinessId", "Sku").IsUnique();
            b.ToTable("products");
        });

        modelBuilder.Entity("BillingRD.Domain.Customers.Customer", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Email").HasMaxLength(320).HasColumnType("character varying(320)");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("Phone").HasMaxLength(40).HasColumnType("character varying(40)");
            b.Property<string>("TaxId").HasMaxLength(20).HasColumnType("character varying(20)");
            b.HasKey("Id");
            b.HasIndex("BusinessId", "TaxId").IsUnique().HasFilter(@"""TaxId"" IS NOT NULL");
            b.ToTable("customers");
        });

        modelBuilder.Entity("BillingRD.Domain.Identity.BusinessMembership", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Role").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<Guid>("UserId").HasColumnType("uuid");
            b.HasKey("Id");
            b.HasIndex("BusinessId", "UserId").IsUnique();
            b.HasIndex("UserId");
            b.ToTable("business_memberships");
        });

        modelBuilder.Entity("BillingRD.Domain.Identity.UserAccount", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Email").IsRequired().HasMaxLength(320).HasColumnType("character varying(320)");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("PasswordHash").IsRequired().HasMaxLength(512).HasColumnType("character varying(512)");
            b.HasKey("Id");
            b.HasIndex("Email").IsUnique();
            b.ToTable("users");
        });

        modelBuilder.Entity("BillingRD.Domain.Cash.CashMovement", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<decimal>("Amount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<Guid>("CashSessionId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("CreatedByUserId").HasColumnType("uuid");
            b.Property<Guid?>("PaymentId").HasColumnType("uuid");
            b.Property<string>("Reason").HasMaxLength(240).HasColumnType("character varying(240)");
            b.Property<Guid?>("RefundId").HasColumnType("uuid");
            b.Property<Guid?>("SaleId").HasColumnType("uuid");
            b.Property<string>("Type").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.HasKey("Id");
            b.HasIndex("BusinessId");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("RefundId").IsUnique().HasFilter(@"""RefundId"" IS NOT NULL");
            b.HasIndex("SaleId");
            b.HasIndex("CashSessionId", "CreatedAtUtc");
            b.HasIndex("PaymentId").IsUnique().HasFilter(@"""PaymentId"" IS NOT NULL");
            b.ToTable("cash_movements");
        });

        modelBuilder.Entity("BillingRD.Domain.Cash.CashRegister", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BranchId").HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<bool>("IsActive").HasColumnType("boolean");
            b.Property<string>("Name").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
            b.HasKey("Id");
            b.HasIndex("BranchId");
            b.HasIndex("BusinessId", "BranchId", "Name").IsUnique();
            b.ToTable("cash_registers");
        });

        modelBuilder.Entity("BillingRD.Domain.Cash.CashSession", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<Guid>("CashRegisterId").HasColumnType("uuid");
            b.Property<Guid?>("ClosedByUserId").HasColumnType("uuid");
            b.Property<DateTimeOffset?>("ClosedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<decimal?>("CountedCash").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal?>("Difference").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal?>("ExpectedCashAtClose").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("OpeningBalance").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<DateTimeOffset>("OpenedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("OpenedByUserId").HasColumnType("uuid");
            b.HasKey("Id");
            b.HasIndex("BusinessId");
            b.HasIndex("ClosedByUserId");
            b.HasIndex("OpenedByUserId");
            b.HasIndex("CashRegisterId").IsUnique().HasFilter(@"""ClosedAtUtc"" IS NULL");
            b.ToTable("cash_sessions");
        });

        modelBuilder.Entity("BillingRD.Domain.Inventory.StockBalance", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BranchId").HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<Guid>("ProductId").HasColumnType("uuid");
            b.Property<decimal>("Quantity").HasPrecision(18, 3).HasColumnType("numeric(18,3)");
            b.Property<DateTimeOffset>("UpdatedAtUtc").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("BranchId");
            b.HasIndex("ProductId");
            b.HasIndex("BusinessId", "BranchId", "ProductId").IsUnique();
            b.ToTable("stock_balances");
        });

        modelBuilder.Entity("BillingRD.Domain.Inventory.StockMovement", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<decimal>("BalanceAfter").HasPrecision(18, 3).HasColumnType("numeric(18,3)");
            b.Property<Guid>("BranchId").HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("CreatedByUserId").HasColumnType("uuid");
            b.Property<Guid>("ProductId").HasColumnType("uuid");
            b.Property<decimal>("QuantityDelta").HasPrecision(18, 3).HasColumnType("numeric(18,3)");
            b.Property<string>("Reason").HasMaxLength(240).HasColumnType("character varying(240)");
            b.Property<Guid?>("ReturnId").HasColumnType("uuid");
            b.Property<Guid?>("SaleId").HasColumnType("uuid");
            b.Property<string>("Type").IsRequired().HasMaxLength(24).HasColumnType("character varying(24)");
            b.HasKey("Id");
            b.HasIndex("BusinessId");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("ProductId");
            b.HasIndex("BranchId", "ProductId", "CreatedAtUtc");
            b.HasIndex("ReturnId", "ProductId").IsUnique().HasFilter(@"""ReturnId"" IS NOT NULL");
            b.HasIndex("SaleId", "ProductId").IsUnique().HasFilter(@"""SaleId"" IS NOT NULL AND ""ReturnId"" IS NULL");
            b.ToTable("stock_movements");
        });

        modelBuilder.Entity("BillingRD.Domain.Payments.Payment", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<decimal>("Amount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Method").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("Reference").HasMaxLength(160).HasColumnType("character varying(160)");
            b.Property<Guid>("SaleId").HasColumnType("uuid");
            b.HasKey("Id");
            b.HasIndex("BusinessId");
            b.HasIndex("SaleId");
            b.ToTable("payments");
        });

        modelBuilder.Entity("BillingRD.Domain.Returns.Refund", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<decimal>("Amount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("Method").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("Reference").HasMaxLength(160).HasColumnType("character varying(160)");
            b.Property<Guid>("ReturnId").HasColumnType("uuid");
            b.Property<Guid>("SaleId").HasColumnType("uuid");
            b.HasKey("Id");
            b.HasIndex("BusinessId");
            b.HasIndex("ReturnId");
            b.HasIndex("SaleId");
            b.ToTable("refunds");
        });

        modelBuilder.Entity("BillingRD.Domain.Returns.ReturnLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<string>("ItbisCategory").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)");
            b.Property<Guid>("ProductId").HasColumnType("uuid");
            b.Property<string>("ProductName").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<decimal>("Quantity").HasPrecision(18, 3).HasColumnType("numeric(18,3)");
            b.Property<Guid>("ReturnId").HasColumnType("uuid");
            b.Property<Guid>("SaleId").HasColumnType("uuid");
            b.Property<Guid>("SaleLineId").HasColumnType("uuid");
            b.Property<string>("Sku").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<decimal>("Subtotal").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("TaxAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("TaxRate").HasPrecision(5, 2).HasColumnType("numeric(5,2)");
            b.Property<decimal>("Total").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("UnitPrice").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.HasKey("Id");
            b.HasIndex("BusinessId");
            b.HasIndex("ProductId");
            b.HasIndex("ReturnId");
            b.HasIndex("SaleId");
            b.HasIndex("SaleLineId");
            b.ToTable("return_lines");
        });

        modelBuilder.Entity("BillingRD.Domain.Returns.SalesReturn", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BranchId").HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("CreatedByUserId").HasColumnType("uuid");
            b.Property<string>("IdempotencyKey").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<string>("Reason").IsRequired().HasMaxLength(240).HasColumnType("character varying(240)");
            b.Property<Guid>("SaleId").HasColumnType("uuid");
            b.Property<decimal>("Subtotal").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("TaxAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("Total").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.HasKey("Id");
            b.HasIndex("BranchId");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("SaleId");
            b.HasIndex("BusinessId", "IdempotencyKey").IsUnique();
            b.ToTable("returns");
        });

        modelBuilder.Entity("BillingRD.Domain.Adjustments.AdjustmentDocument", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("CreatedByUserId").HasColumnType("uuid");
            b.Property<string>("Kind").IsRequired().HasMaxLength(24).HasColumnType("character varying(24)");
            b.Property<string>("Reason").IsRequired().HasMaxLength(240).HasColumnType("character varying(240)");
            b.Property<Guid>("ReturnId").HasColumnType("uuid");
            b.Property<Guid>("SaleId").HasColumnType("uuid");
            b.Property<decimal>("Subtotal").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("TaxAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("Total").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.HasKey("Id");
            b.HasIndex("BusinessId");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("ReturnId").IsUnique();
            b.HasIndex("SaleId");
            b.ToTable("adjustment_documents");
        });

        modelBuilder.Entity("BillingRD.Domain.ElectronicInvoicing.ElectronicFiscalDocumentDraft", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("AdjustmentDocumentId").HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("CreatedByUserId").HasColumnType("uuid");
            b.Property<Guid>("ReturnId").HasColumnType("uuid");
            b.Property<Guid>("SaleId").HasColumnType("uuid");
            b.Property<decimal>("Subtotal").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("TaxAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("Total").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<int>("Type").HasColumnType("integer");
            b.HasKey("Id");
            b.HasIndex("AdjustmentDocumentId").IsUnique();
            b.HasIndex("BusinessId");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("ReturnId").IsUnique();
            b.HasIndex("SaleId");
            b.ToTable("electronic_fiscal_document_drafts");
        });

        modelBuilder.Entity("BillingRD.Domain.Sales.Sale", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BranchId").HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<Guid>("CreatedByUserId").HasColumnType("uuid");
            b.Property<Guid?>("CustomerId").HasColumnType("uuid");
            b.Property<string>("IdempotencyKey").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<decimal>("Subtotal").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("TaxAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("Total").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.HasKey("Id");
            b.HasIndex("BranchId");
            b.HasIndex("CreatedByUserId");
            b.HasIndex("CustomerId");
            b.HasIndex("BusinessId", "IdempotencyKey").IsUnique();
            b.ToTable("sales");
        });

        modelBuilder.Entity("BillingRD.Domain.Sales.SaleLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
            b.Property<Guid>("BusinessId").HasColumnType("uuid");
            b.Property<string>("ItbisCategory").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)");
            b.Property<Guid>("ProductId").HasColumnType("uuid");
            b.Property<string>("ProductName").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<decimal>("Quantity").HasPrecision(18, 3).HasColumnType("numeric(18,3)");
            b.Property<Guid>("SaleId").HasColumnType("uuid");
            b.Property<string>("Sku").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
            b.Property<decimal>("Subtotal").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("TaxAmount").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("TaxRate").HasPrecision(5, 2).HasColumnType("numeric(5,2)");
            b.Property<decimal>("Total").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.Property<decimal>("UnitPrice").HasPrecision(18, 2).HasColumnType("numeric(18,2)");
            b.HasKey("Id");
            b.HasIndex("BusinessId");
            b.HasIndex("ProductId");
            b.HasIndex("SaleId");
            b.ToTable("sale_lines");
        });

        modelBuilder.Entity("BillingRD.Domain.Billing.Invoice", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Customers.Customer", null)
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("BillingRD.Domain.Sales.Sale", null)
                .WithMany()
                .HasForeignKey("SaleId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Businesses.Branch", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Catalog.Product", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Customers.Customer", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Identity.BusinessMembership", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Identity.UserAccount", null)
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Cash.CashMovement", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Cash.CashSession", null)
                .WithMany()
                .HasForeignKey("CashSessionId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Identity.UserAccount", null)
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Payments.Payment", null)
                .WithMany()
                .HasForeignKey("PaymentId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("BillingRD.Domain.Returns.Refund", null)
                .WithMany()
                .HasForeignKey("RefundId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("BillingRD.Domain.Sales.Sale", null)
                .WithMany()
                .HasForeignKey("SaleId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity("BillingRD.Domain.Cash.CashRegister", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Branch", null)
                .WithMany()
                .HasForeignKey("BranchId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Cash.CashSession", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Cash.CashRegister", null)
                .WithMany()
                .HasForeignKey("CashRegisterId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Identity.UserAccount", null)
                .WithMany()
                .HasForeignKey("ClosedByUserId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("BillingRD.Domain.Identity.UserAccount", null)
                .WithMany()
                .HasForeignKey("OpenedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Inventory.StockBalance", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Branch", null)
                .WithMany()
                .HasForeignKey("BranchId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Catalog.Product", null)
                .WithMany()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Inventory.StockMovement", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Branch", null)
                .WithMany()
                .HasForeignKey("BranchId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Identity.UserAccount", null)
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Catalog.Product", null)
                .WithMany()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Returns.SalesReturn", null)
                .WithMany()
                .HasForeignKey("ReturnId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("BillingRD.Domain.Sales.Sale", null)
                .WithMany()
                .HasForeignKey("SaleId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity("BillingRD.Domain.Payments.Payment", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Sales.Sale", null)
                .WithMany()
                .HasForeignKey("SaleId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Returns.Refund", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Returns.SalesReturn", null)
                .WithMany("Refunds")
                .HasForeignKey("ReturnId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Sales.Sale", null)
                .WithMany()
                .HasForeignKey("SaleId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Returns.ReturnLine", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Catalog.Product", null)
                .WithMany()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Returns.SalesReturn", null)
                .WithMany("Lines")
                .HasForeignKey("ReturnId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Sales.Sale", null)
                .WithMany()
                .HasForeignKey("SaleId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Sales.SaleLine", null)
                .WithMany()
                .HasForeignKey("SaleLineId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Returns.SalesReturn", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Branch", null)
                .WithMany()
                .HasForeignKey("BranchId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Identity.UserAccount", null)
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Sales.Sale", null)
                .WithMany()
                .HasForeignKey("SaleId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Adjustments.AdjustmentDocument", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Identity.UserAccount", null)
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Returns.SalesReturn", null)
                .WithMany()
                .HasForeignKey("ReturnId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Sales.Sale", null)
                .WithMany()
                .HasForeignKey("SaleId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.ElectronicInvoicing.ElectronicFiscalDocumentDraft", b =>
        {
            b.HasOne("BillingRD.Domain.Adjustments.AdjustmentDocument", null)
                .WithMany()
                .HasForeignKey("AdjustmentDocumentId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Identity.UserAccount", null)
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Returns.SalesReturn", null)
                .WithMany()
                .HasForeignKey("ReturnId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Sales.Sale", null)
                .WithMany()
                .HasForeignKey("SaleId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Sales.Sale", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Branch", null)
                .WithMany()
                .HasForeignKey("BranchId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Customers.Customer", null)
                .WithMany()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("BillingRD.Domain.Identity.UserAccount", null)
                .WithMany()
                .HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("BillingRD.Domain.Sales.SaleLine", b =>
        {
            b.HasOne("BillingRD.Domain.Businesses.Business", null)
                .WithMany()
                .HasForeignKey("BusinessId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Catalog.Product", null)
                .WithMany()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("BillingRD.Domain.Sales.Sale", null)
                .WithMany("Lines")
                .HasForeignKey("SaleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

        });

        modelBuilder.Entity("BillingRD.Domain.Returns.SalesReturn", b =>
        {
            b.Navigation("Lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            b.Navigation("Refunds")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity("BillingRD.Domain.Sales.Sale", b =>
        {
            b.Navigation("Lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
