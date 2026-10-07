using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingRD.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
[Migration("20261007100000_AddFiscalProfilesAndInvoiceDrafts")]
public partial class AddFiscalProfilesAndInvoiceDrafts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ForeignIdentifier",
            table: "customers",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FiscalAddress",
            table: "customers",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "TaxId",
            table: "customers",
            type: "character varying(11)",
            maxLength: 11,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true);

        migrationBuilder.CreateTable(
            name: "business_fiscal_profiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Rnc = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                LegalName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                TradeName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                Address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_business_fiscal_profiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_business_fiscal_profiles_businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.DropForeignKey(
            name: "FK_electronic_fiscal_document_drafts_adjustment_documents_AdjustmentDocumentId",
            table: "electronic_fiscal_document_drafts");

        migrationBuilder.DropForeignKey(
            name: "FK_electronic_fiscal_document_drafts_returns_ReturnId",
            table: "electronic_fiscal_document_drafts");

        migrationBuilder.DropIndex(
            name: "IX_electronic_fiscal_document_drafts_AdjustmentDocumentId",
            table: "electronic_fiscal_document_drafts");

        migrationBuilder.DropIndex(
            name: "IX_electronic_fiscal_document_drafts_ReturnId",
            table: "electronic_fiscal_document_drafts");

        migrationBuilder.AlterColumn<Guid>(
            name: "AdjustmentDocumentId",
            table: "electronic_fiscal_document_drafts",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "ReturnId",
            table: "electronic_fiscal_document_drafts",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<Guid>(
            name: "InvoiceId",
            table: "electronic_fiscal_document_drafts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "CustomerId",
            table: "electronic_fiscal_document_drafts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IssuerRnc",
            table: "electronic_fiscal_document_drafts",
            type: "character varying(11)",
            maxLength: 11,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IssuerLegalName",
            table: "electronic_fiscal_document_drafts",
            type: "character varying(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IssuerTradeName",
            table: "electronic_fiscal_document_drafts",
            type: "character varying(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IssuerAddress",
            table: "electronic_fiscal_document_drafts",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BuyerTaxId",
            table: "electronic_fiscal_document_drafts",
            type: "character varying(11)",
            maxLength: 11,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BuyerForeignIdentifier",
            table: "electronic_fiscal_document_drafts",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BuyerName",
            table: "electronic_fiscal_document_drafts",
            type: "character varying(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BuyerAddress",
            table: "electronic_fiscal_document_drafts",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_business_fiscal_profiles_BusinessId",
            table: "business_fiscal_profiles",
            column: "BusinessId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_business_fiscal_profiles_Rnc",
            table: "business_fiscal_profiles",
            column: "Rnc",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_electronic_fiscal_document_drafts_InvoiceId",
            table: "electronic_fiscal_document_drafts",
            column: "InvoiceId",
            unique: true,
            filter: @"""InvoiceId"" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_electronic_fiscal_document_drafts_CustomerId",
            table: "electronic_fiscal_document_drafts",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_electronic_fiscal_document_drafts_AdjustmentDocumentId",
            table: "electronic_fiscal_document_drafts",
            column: "AdjustmentDocumentId",
            unique: true,
            filter: @"""AdjustmentDocumentId"" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_electronic_fiscal_document_drafts_ReturnId",
            table: "electronic_fiscal_document_drafts",
            column: "ReturnId",
            unique: true,
            filter: @"""ReturnId"" IS NOT NULL");

        migrationBuilder.AddForeignKey(
            name: "FK_electronic_fiscal_document_drafts_adjustment_documents_AdjustmentDocumentId",
            table: "electronic_fiscal_document_drafts",
            column: "AdjustmentDocumentId",
            principalTable: "adjustment_documents",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_electronic_fiscal_document_drafts_returns_ReturnId",
            table: "electronic_fiscal_document_drafts",
            column: "ReturnId",
            principalTable: "returns",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_electronic_fiscal_document_drafts_invoices_InvoiceId",
            table: "electronic_fiscal_document_drafts",
            column: "InvoiceId",
            principalTable: "invoices",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_electronic_fiscal_document_drafts_customers_CustomerId",
            table: "electronic_fiscal_document_drafts",
            column: "CustomerId",
            principalTable: "customers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_electronic_fiscal_document_drafts_invoices_InvoiceId", "electronic_fiscal_document_drafts");
        migrationBuilder.DropForeignKey("FK_electronic_fiscal_document_drafts_customers_CustomerId", "electronic_fiscal_document_drafts");
        migrationBuilder.DropForeignKey("FK_electronic_fiscal_document_drafts_adjustment_documents_AdjustmentDocumentId", "electronic_fiscal_document_drafts");
        migrationBuilder.DropForeignKey("FK_electronic_fiscal_document_drafts_returns_ReturnId", "electronic_fiscal_document_drafts");

        migrationBuilder.DropTable("business_fiscal_profiles");

        migrationBuilder.DropIndex("IX_electronic_fiscal_document_drafts_InvoiceId", "electronic_fiscal_document_drafts");
        migrationBuilder.DropIndex("IX_electronic_fiscal_document_drafts_CustomerId", "electronic_fiscal_document_drafts");
        migrationBuilder.DropIndex("IX_electronic_fiscal_document_drafts_AdjustmentDocumentId", "electronic_fiscal_document_drafts");
        migrationBuilder.DropIndex("IX_electronic_fiscal_document_drafts_ReturnId", "electronic_fiscal_document_drafts");

        migrationBuilder.DropColumn("InvoiceId", "electronic_fiscal_document_drafts");
        migrationBuilder.DropColumn("CustomerId", "electronic_fiscal_document_drafts");
        migrationBuilder.DropColumn("IssuerRnc", "electronic_fiscal_document_drafts");
        migrationBuilder.DropColumn("IssuerLegalName", "electronic_fiscal_document_drafts");
        migrationBuilder.DropColumn("IssuerTradeName", "electronic_fiscal_document_drafts");
        migrationBuilder.DropColumn("IssuerAddress", "electronic_fiscal_document_drafts");
        migrationBuilder.DropColumn("BuyerTaxId", "electronic_fiscal_document_drafts");
        migrationBuilder.DropColumn("BuyerForeignIdentifier", "electronic_fiscal_document_drafts");
        migrationBuilder.DropColumn("BuyerName", "electronic_fiscal_document_drafts");
        migrationBuilder.DropColumn("BuyerAddress", "electronic_fiscal_document_drafts");

        migrationBuilder.AlterColumn<Guid>(
            name: "AdjustmentDocumentId",
            table: "electronic_fiscal_document_drafts",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "ReturnId",
            table: "electronic_fiscal_document_drafts",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_electronic_fiscal_document_drafts_AdjustmentDocumentId",
            table: "electronic_fiscal_document_drafts",
            column: "AdjustmentDocumentId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_electronic_fiscal_document_drafts_ReturnId",
            table: "electronic_fiscal_document_drafts",
            column: "ReturnId",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_electronic_fiscal_document_drafts_adjustment_documents_AdjustmentDocumentId",
            table: "electronic_fiscal_document_drafts",
            column: "AdjustmentDocumentId",
            principalTable: "adjustment_documents",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_electronic_fiscal_document_drafts_returns_ReturnId",
            table: "electronic_fiscal_document_drafts",
            column: "ReturnId",
            principalTable: "returns",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.DropColumn("ForeignIdentifier", "customers");
        migrationBuilder.DropColumn("FiscalAddress", "customers");

        migrationBuilder.AlterColumn<string>(
            name: "TaxId",
            table: "customers",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(11)",
            oldMaxLength: 11,
            oldNullable: true);
    }
}
