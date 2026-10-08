using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingRD.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
[Migration("20261008103000_AddFiscalDraftSnapshots")]
public partial class AddFiscalDraftSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Kind",
            table: "products",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Unknown");

        migrationBuilder.AddColumn<string>(
            name: "ProductKind",
            table: "sale_lines",
            type: "character varying(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "Unknown");

        migrationBuilder.AddColumn<DateOnly>(
            name: "FiscalIssueDate",
            table: "electronic_fiscal_document_drafts",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IncomeType",
            table: "electronic_fiscal_document_drafts",
            type: "character varying(2)",
            maxLength: 2,
            nullable: false,
            defaultValue: "01");

        migrationBuilder.AddColumn<int>(
            name: "PaymentType",
            table: "electronic_fiscal_document_drafts",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        foreach (var column in new[] { "TaxableAmount18", "TaxableAmount16", "TaxableAmount0", "ExemptAmount", "Tax18", "Tax16" })
        {
            migrationBuilder.AddColumn<decimal>(
                name: column,
                table: "electronic_fiscal_document_drafts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        migrationBuilder.CreateTable(
            name: "fiscal_draft_lines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                Sku = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                ProductKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                BillingIndicator = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                UnitPrice = table.Column<decimal>(type: "numeric(20,4)", precision: 20, scale: 4, nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TaxRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fiscal_draft_lines", x => x.Id);
                table.ForeignKey(
                    name: "FK_fiscal_draft_lines_businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_fiscal_draft_lines_electronic_fiscal_document_drafts_DraftId",
                    column: x => x.DraftId,
                    principalTable: "electronic_fiscal_document_drafts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "fiscal_draft_payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                SourcePaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                FormCode = table.Column<int>(type: "integer", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fiscal_draft_payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_fiscal_draft_payments_businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_fiscal_draft_payments_electronic_fiscal_document_drafts_DraftId",
                    column: x => x.DraftId,
                    principalTable: "electronic_fiscal_document_drafts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_fiscal_draft_lines_BusinessId",
            table: "fiscal_draft_lines",
            column: "BusinessId");

        migrationBuilder.CreateIndex(
            name: "IX_fiscal_draft_lines_DraftId_Number",
            table: "fiscal_draft_lines",
            columns: new[] { "DraftId", "Number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_fiscal_draft_payments_BusinessId",
            table: "fiscal_draft_payments",
            column: "BusinessId");

        migrationBuilder.CreateIndex(
            name: "IX_fiscal_draft_payments_DraftId",
            table: "fiscal_draft_payments",
            column: "DraftId");

        migrationBuilder.CreateIndex(
            name: "IX_fiscal_draft_payments_SourcePaymentId",
            table: "fiscal_draft_payments",
            column: "SourcePaymentId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "fiscal_draft_lines");
        migrationBuilder.DropTable(name: "fiscal_draft_payments");

        migrationBuilder.DropColumn(name: "Kind", table: "products");
        migrationBuilder.DropColumn(name: "ProductKind", table: "sale_lines");

        foreach (var column in new[]
        {
            "FiscalIssueDate", "IncomeType", "PaymentType", "TaxableAmount18",
            "TaxableAmount16", "TaxableAmount0", "ExemptAmount", "Tax18", "Tax16"
        })
        {
            migrationBuilder.DropColumn(name: column, table: "electronic_fiscal_document_drafts");
        }
    }
}
