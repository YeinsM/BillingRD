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

        modelBuilder.Entity("BillingRD.Domain.Sales.Sale", b =>
        {
            b.Navigation("Lines");
        });
    }
}
