using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingRD.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
[Migration("20261006170000_AddAdjustmentsAndEcfDrafts")]
public partial class AddAdjustmentsAndEcfDrafts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "adjustment_documents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                ReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Reason = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_adjustment_documents", x => x.Id);
                table.ForeignKey("FK_adjustment_documents_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_adjustment_documents_returns_ReturnId", x => x.ReturnId, "returns", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_adjustment_documents_sales_SaleId", x => x.SaleId, "sales", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_adjustment_documents_users_CreatedByUserId", x => x.CreatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "electronic_fiscal_document_drafts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                AdjustmentDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                ReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_electronic_fiscal_document_drafts", x => x.Id);
                table.ForeignKey("FK_electronic_fiscal_document_drafts_adjustment_documents_AdjustmentDocumentId", x => x.AdjustmentDocumentId, "adjustment_documents", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_electronic_fiscal_document_drafts_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_electronic_fiscal_document_drafts_returns_ReturnId", x => x.ReturnId, "returns", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_electronic_fiscal_document_drafts_sales_SaleId", x => x.SaleId, "sales", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_electronic_fiscal_document_drafts_users_CreatedByUserId", x => x.CreatedByUserId, "users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name:"IX_adjustment_documents_BusinessId",table:"adjustment_documents",column:"BusinessId");
        migrationBuilder.CreateIndex(name:"IX_adjustment_documents_CreatedByUserId",table:"adjustment_documents",column:"CreatedByUserId");
        migrationBuilder.CreateIndex(name:"IX_adjustment_documents_SaleId",table:"adjustment_documents",column:"SaleId");
        migrationBuilder.CreateIndex(name:"IX_adjustment_documents_ReturnId",table:"adjustment_documents",column:"ReturnId",unique:true);

        migrationBuilder.CreateIndex(name:"IX_electronic_fiscal_document_drafts_BusinessId",table:"electronic_fiscal_document_drafts",column:"BusinessId");
        migrationBuilder.CreateIndex(name:"IX_electronic_fiscal_document_drafts_CreatedByUserId",table:"electronic_fiscal_document_drafts",column:"CreatedByUserId");
        migrationBuilder.CreateIndex(name:"IX_electronic_fiscal_document_drafts_SaleId",table:"electronic_fiscal_document_drafts",column:"SaleId");
        migrationBuilder.CreateIndex(name:"IX_electronic_fiscal_document_drafts_ReturnId",table:"electronic_fiscal_document_drafts",column:"ReturnId",unique:true);
        migrationBuilder.CreateIndex(name:"IX_electronic_fiscal_document_drafts_AdjustmentDocumentId",table:"electronic_fiscal_document_drafts",column:"AdjustmentDocumentId",unique:true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("electronic_fiscal_document_drafts");
        migrationBuilder.DropTable("adjustment_documents");
    }
}
