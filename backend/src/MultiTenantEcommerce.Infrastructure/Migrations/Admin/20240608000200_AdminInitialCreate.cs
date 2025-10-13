using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MultiTenantEcommerce.Infrastructure.Persistence;

#nullable disable

namespace MultiTenantEcommerce.Infrastructure.Migrations.Admin
{
    [DbContext(typeof(AdminDbContext))]
    public partial class AdminInitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CustomDomain = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DbConnectionString = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    PlanId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CustomDomain",
                table: "Tenants",
                column: "CustomDomain",
                unique: true,
                filter: "[CustomDomain] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
