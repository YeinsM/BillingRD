using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingRD.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
[Migration("20261006120500_AddSalesInvoicesPayments")]
public partial class AddSalesInvoicesPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ItbisCategory",
            table: "products",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Standard");

        migrationBuilder.CreateTable(
            name: "sales",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sales", x => x.Id);
                table.ForeignKey("FK_sales_branches_BranchId", x => x.BranchId, "branches", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_sales_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_sales_customers_CustomerId", x => x.CustomerId, "customers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_sales_users_CreatedByUserId", x => x.CreatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "sale_lines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                ItbisCategory = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                TaxRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sale_lines", x => x.Id);
                table.ForeignKey("FK_sale_lines_products_ProductId", x => x.ProductId, "products", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_sale_lines_sales_SaleId", x => x.SaleId, "sales", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "invoices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_invoices", x => x.Id);
                table.ForeignKey("FK_invoices_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_invoices_customers_CustomerId", x => x.CustomerId, "customers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_invoices_sales_SaleId", x => x.SaleId, "sales", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                Method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_payments", x => x.Id);
                table.ForeignKey("FK_payments_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_payments_sales_SaleId", x => x.SaleId, "sales", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_sales_BranchId", "sales", "BranchId");
        migrationBuilder.CreateIndex("IX_sales_BusinessId_IdempotencyKey", "sales", new[] { "BusinessId", "IdempotencyKey" }, unique: true);
        migrationBuilder.CreateIndex("IX_sales_CreatedByUserId", "sales", "CreatedByUserId");
        migrationBuilder.CreateIndex("IX_sales_CustomerId", "sales", "CustomerId");

        migrationBuilder.CreateIndex("IX_sale_lines_ProductId", "sale_lines", "ProductId");
        migrationBuilder.CreateIndex("IX_sale_lines_SaleId", "sale_lines", "SaleId");

        migrationBuilder.CreateIndex("IX_invoices_BusinessId", "invoices", "BusinessId");
        migrationBuilder.CreateIndex("IX_invoices_CustomerId", "invoices", "CustomerId");
        migrationBuilder.CreateIndex("IX_invoices_SaleId", "invoices", "SaleId", unique: true);

        migrationBuilder.CreateIndex("IX_payments_BusinessId", "payments", "BusinessId");
        migrationBuilder.CreateIndex("IX_payments_SaleId", "payments", "SaleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("invoices");
        migrationBuilder.DropTable("payments");
        migrationBuilder.DropTable("sale_lines");
        migrationBuilder.DropTable("sales");

        migrationBuilder.DropColumn(
            name: "ItbisCategory",
            table: "products");
    }
}
