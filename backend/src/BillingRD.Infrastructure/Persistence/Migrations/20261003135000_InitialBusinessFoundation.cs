using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillingRD.Infrastructure.Persistence.Migrations;

/// <summary>
/// Creates the first multi-business foundation tables.
/// </summary>
public partial class InitialBusinessFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "businesses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                TaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_businesses", x => x.Id));

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_users", x => x.Id));

        migrationBuilder.CreateTable(
            name: "branches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_branches", x => x.Id);
                table.ForeignKey("FK_branches_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                TaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_customers", x => x.Id);
                table.ForeignKey("FK_customers_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "products",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Barcode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                SalePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_products", x => x.Id);
                table.ForeignKey("FK_products_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "business_memberships",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_business_memberships", x => x.Id);
                table.ForeignKey("FK_business_memberships_businesses_BusinessId", x => x.BusinessId, "businesses", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_business_memberships_users_UserId", x => x.UserId, "users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_businesses_TaxId", "businesses", "TaxId", unique: true, filter: ""TaxId" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_users_Email", "users", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_branches_BusinessId_Name", "branches", new[] { "BusinessId", "Name" }, unique: true);
        migrationBuilder.CreateIndex("IX_customers_BusinessId_TaxId", "customers", new[] { "BusinessId", "TaxId" }, unique: true, filter: ""TaxId" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_products_BusinessId_Sku", "products", new[] { "BusinessId", "Sku" }, unique: true);
        migrationBuilder.CreateIndex("IX_products_BusinessId_Barcode", "products", new[] { "BusinessId", "Barcode" }, unique: true, filter: ""Barcode" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_business_memberships_BusinessId_UserId", "business_memberships", new[] { "BusinessId", "UserId" }, unique: true);
        migrationBuilder.CreateIndex("IX_business_memberships_UserId", "business_memberships", "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("branches");
        migrationBuilder.DropTable("business_memberships");
        migrationBuilder.DropTable("customers");
        migrationBuilder.DropTable("products");
        migrationBuilder.DropTable("users");
        migrationBuilder.DropTable("businesses");
    }
}
