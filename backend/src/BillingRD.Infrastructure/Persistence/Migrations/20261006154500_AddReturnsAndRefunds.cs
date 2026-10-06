using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingRD.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
[Migration("20261006154500_AddReturnsAndRefunds")]
public partial class AddReturnsAndRefunds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ReturnId",
            table: "stock_movements",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "RefundId",
            table: "cash_movements",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "returns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Reason = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_returns", x => x.Id);
                table.ForeignKey("FK_returns_branches_BranchId", x => x.BranchId, "branches", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_returns_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_returns_sales_SaleId", x => x.SaleId, "sales", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_returns_users_CreatedByUserId", x => x.CreatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "return_lines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                ReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                SaleLineId = table.Column<Guid>(type: "uuid", nullable: false),
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
                table.PrimaryKey("PK_return_lines", x => x.Id);
                table.ForeignKey("FK_return_lines_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_return_lines_products_ProductId", x => x.ProductId, "products", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_return_lines_returns_ReturnId", x => x.ReturnId, "returns", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_return_lines_sale_lines_SaleLineId", x => x.SaleLineId, "sale_lines", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_return_lines_sales_SaleId", x => x.SaleId, "sales", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "refunds",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                ReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                Method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_refunds", x => x.Id);
                table.ForeignKey("FK_refunds_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_refunds_returns_ReturnId", x => x.ReturnId, "returns", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_refunds_sales_SaleId", x => x.SaleId, "sales", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.DropIndex(
            name: "IX_stock_movements_SaleId_ProductId",
            table: "stock_movements");

        migrationBuilder.CreateIndex(
            name: "IX_stock_movements_SaleId_ProductId",
            table: "stock_movements",
            columns: new[] { "SaleId", "ProductId" },
            unique: true,
            filter: @"""SaleId"" IS NOT NULL AND ""ReturnId"" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_stock_movements_ReturnId_ProductId",
            table: "stock_movements",
            columns: new[] { "ReturnId", "ProductId" },
            unique: true,
            filter: @"""ReturnId"" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_cash_movements_RefundId",
            table: "cash_movements",
            column: "RefundId",
            unique: true,
            filter: @"""RefundId"" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_returns_BranchId",
            table: "returns",
            column: "BranchId");

        migrationBuilder.CreateIndex(
            name: "IX_returns_CreatedByUserId",
            table: "returns",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_returns_SaleId",
            table: "returns",
            column: "SaleId");

        migrationBuilder.CreateIndex(
            name: "IX_returns_BusinessId_IdempotencyKey",
            table: "returns",
            columns: new[] { "BusinessId", "IdempotencyKey" },
            unique: true);

        migrationBuilder.CreateIndex(name: "IX_return_lines_BusinessId", table: "return_lines", column: "BusinessId");
        migrationBuilder.CreateIndex(name: "IX_return_lines_ProductId", table: "return_lines", column: "ProductId");
        migrationBuilder.CreateIndex(name: "IX_return_lines_ReturnId", table: "return_lines", column: "ReturnId");
        migrationBuilder.CreateIndex(name: "IX_return_lines_SaleId", table: "return_lines", column: "SaleId");
        migrationBuilder.CreateIndex(name: "IX_return_lines_SaleLineId", table: "return_lines", column: "SaleLineId");

        migrationBuilder.CreateIndex(name: "IX_refunds_BusinessId", table: "refunds", column: "BusinessId");
        migrationBuilder.CreateIndex(name: "IX_refunds_ReturnId", table: "refunds", column: "ReturnId");
        migrationBuilder.CreateIndex(name: "IX_refunds_SaleId", table: "refunds", column: "SaleId");

        migrationBuilder.AddForeignKey(
            name: "FK_stock_movements_returns_ReturnId",
            table: "stock_movements",
            column: "ReturnId",
            principalTable: "returns",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_cash_movements_refunds_RefundId",
            table: "cash_movements",
            column: "RefundId",
            principalTable: "refunds",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_stock_movements_returns_ReturnId", "stock_movements");
        migrationBuilder.DropForeignKey("FK_cash_movements_refunds_RefundId", "cash_movements");

        migrationBuilder.DropIndex("IX_stock_movements_ReturnId_ProductId", "stock_movements");
        migrationBuilder.DropIndex("IX_cash_movements_RefundId", "cash_movements");

        migrationBuilder.DropIndex("IX_stock_movements_SaleId_ProductId", "stock_movements");
        migrationBuilder.CreateIndex(
            name: "IX_stock_movements_SaleId_ProductId",
            table: "stock_movements",
            columns: new[] { "SaleId", "ProductId" },
            unique: true,
            filter: @"""SaleId"" IS NOT NULL");

        migrationBuilder.DropTable("return_lines");
        migrationBuilder.DropTable("refunds");
        migrationBuilder.DropTable("returns");

        migrationBuilder.DropColumn("ReturnId", "stock_movements");
        migrationBuilder.DropColumn("RefundId", "cash_movements");
    }
}
