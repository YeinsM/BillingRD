using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingRD.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
[Migration("20261006131500_AddBranchInventory")]
public partial class AddBranchInventory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "TracksInventory",
            table: "products",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "stock_balances",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stock_balances", x => x.Id);
                table.ForeignKey("FK_stock_balances_branches_BranchId", x => x.BranchId, "branches", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_stock_balances_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_stock_balances_products_ProductId", x => x.ProductId, "products", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "stock_movements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                QuantityDelta = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                BalanceAfter = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: true),
                Reason = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_stock_movements", x => x.Id);
                table.ForeignKey("FK_stock_movements_branches_BranchId", x => x.BranchId, "branches", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_stock_movements_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_stock_movements_products_ProductId", x => x.ProductId, "products", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_stock_movements_sales_SaleId", x => x.SaleId, "sales", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_stock_movements_users_CreatedByUserId", x => x.CreatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_stock_balances_BranchId",
            table: "stock_balances",
            column: "BranchId");

        migrationBuilder.CreateIndex(
            name: "IX_stock_balances_ProductId",
            table: "stock_balances",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_stock_balances_BusinessId_BranchId_ProductId",
            table: "stock_balances",
            columns: new[] { "BusinessId", "BranchId", "ProductId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_stock_movements_BranchId_ProductId_CreatedAtUtc",
            table: "stock_movements",
            columns: new[] { "BranchId", "ProductId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_stock_movements_BusinessId",
            table: "stock_movements",
            column: "BusinessId");

        migrationBuilder.CreateIndex(
            name: "IX_stock_movements_CreatedByUserId",
            table: "stock_movements",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_stock_movements_ProductId",
            table: "stock_movements",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_stock_movements_SaleId_ProductId",
            table: "stock_movements",
            columns: new[] { "SaleId", "ProductId" },
            unique: true,
            filter: @"""SaleId"" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "stock_balances");
        migrationBuilder.DropTable(name: "stock_movements");

        migrationBuilder.DropColumn(
            name: "TracksInventory",
            table: "products");
    }
}
