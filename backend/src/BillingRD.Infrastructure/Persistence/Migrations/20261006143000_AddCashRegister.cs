using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingRD.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
[Migration("20261006143000_AddCashRegister")]
public partial class AddCashRegister : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "cash_registers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cash_registers", x => x.Id);
                table.ForeignKey("FK_cash_registers_branches_BranchId", x => x.BranchId, "branches", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_cash_registers_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "cash_sessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                CashRegisterId = table.Column<Guid>(type: "uuid", nullable: false),
                OpenedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                OpeningBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                OpenedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ClosedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ExpectedCashAtClose = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                CountedCash = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                Difference = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cash_sessions", x => x.Id);
                table.ForeignKey("FK_cash_sessions_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_cash_sessions_cash_registers_CashRegisterId", x => x.CashRegisterId, "cash_registers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_cash_sessions_users_ClosedByUserId", x => x.ClosedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_cash_sessions_users_OpenedByUserId", x => x.OpenedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "cash_movements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                CashSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: true),
                PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                Reason = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cash_movements", x => x.Id);
                table.ForeignKey("FK_cash_movements_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_cash_movements_cash_sessions_CashSessionId", x => x.CashSessionId, "cash_sessions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_cash_movements_payments_PaymentId", x => x.PaymentId, "payments", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_cash_movements_sales_SaleId", x => x.SaleId, "sales", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_cash_movements_users_CreatedByUserId", x => x.CreatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_cash_registers_BranchId",
            table: "cash_registers",
            column: "BranchId");

        migrationBuilder.CreateIndex(
            name: "IX_cash_registers_BusinessId_BranchId_Name",
            table: "cash_registers",
            columns: new[] { "BusinessId", "BranchId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_cash_sessions_BusinessId",
            table: "cash_sessions",
            column: "BusinessId");

        migrationBuilder.CreateIndex(
            name: "IX_cash_sessions_ClosedByUserId",
            table: "cash_sessions",
            column: "ClosedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_cash_sessions_OpenedByUserId",
            table: "cash_sessions",
            column: "OpenedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_cash_sessions_CashRegisterId",
            table: "cash_sessions",
            column: "CashRegisterId",
            unique: true,
            filter: @"""ClosedAtUtc"" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_cash_movements_BusinessId",
            table: "cash_movements",
            column: "BusinessId");

        migrationBuilder.CreateIndex(
            name: "IX_cash_movements_CreatedByUserId",
            table: "cash_movements",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_cash_movements_SaleId",
            table: "cash_movements",
            column: "SaleId");

        migrationBuilder.CreateIndex(
            name: "IX_cash_movements_CashSessionId_CreatedAtUtc",
            table: "cash_movements",
            columns: new[] { "CashSessionId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_cash_movements_PaymentId",
            table: "cash_movements",
            column: "PaymentId",
            unique: true,
            filter: @"""PaymentId"" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("cash_movements");
        migrationBuilder.DropTable("cash_sessions");
        migrationBuilder.DropTable("cash_registers");
    }
}
