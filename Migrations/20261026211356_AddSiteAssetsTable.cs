using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trailblazers.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteAssetsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "site_assets",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Group = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    PublicId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AltText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Caption = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_assets", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_site_assets_Group",
                table: "site_assets",
                column: "Group");

            migrationBuilder.CreateIndex(
                name: "IX_site_assets_Group_SortOrder",
                table: "site_assets",
                columns: new[] { "Group", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "site_assets");
        }
    }
}
