using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingRD.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BillingDbContext))]
[Migration("20261015110000_AddEcfSequenceProvider")]
public partial class AddEcfSequenceProvider : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ecf_sequence_ranges",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Environment = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                FromNumber = table.Column<long>(type: "bigint", nullable: false),
                ToNumber = table.Column<long>(type: "bigint", nullable: false),
                NextNumber = table.Column<long>(type: "bigint", nullable: false),
                ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ecf_sequence_ranges", x => x.Id);
                table.ForeignKey(
                    name: "FK_ecf_sequence_ranges_businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ecf_sequence_allocations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                SequenceRangeId = table.Column<Guid>(type: "uuid", nullable: false),
                Environment = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                ENcf = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                AllocatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ecf_sequence_allocations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ecf_sequence_allocations_businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ecf_sequence_allocations_ecf_sequence_ranges_SequenceRangeId",
                    column: x => x.SequenceRangeId,
                    principalTable: "ecf_sequence_ranges",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ecf_sequence_allocations_electronic_fiscal_document_drafts_DraftId",
                    column: x => x.DraftId,
                    principalTable: "electronic_fiscal_document_drafts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ecf_sequence_ranges_BusinessId_Environment_Type_IsActive",
            table: "ecf_sequence_ranges",
            columns: new[] { "BusinessId", "Environment", "Type", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_ecf_sequence_allocations_BusinessId_Environment_ENcf",
            table: "ecf_sequence_allocations",
            columns: new[] { "BusinessId", "Environment", "ENcf" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ecf_sequence_allocations_DraftId",
            table: "ecf_sequence_allocations",
            column: "DraftId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ecf_sequence_allocations_SequenceRangeId",
            table: "ecf_sequence_allocations",
            column: "SequenceRangeId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ecf_sequence_allocations");
        migrationBuilder.DropTable(name: "ecf_sequence_ranges");
    }
}
