using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trailblazers.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddXminConcurrencyTokenToExamSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "exam_sessions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "exam_sessions");
        }
    }
}
