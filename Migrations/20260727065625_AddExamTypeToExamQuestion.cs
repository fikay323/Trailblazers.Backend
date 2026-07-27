using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trailblazers.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddExamTypeToExamQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exam_questions_aloc_id",
                table: "exam_questions");

            migrationBuilder.AddColumn<string>(
                name: "exam_type",
                table: "exam_questions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"exam_questions\" SET \"exam_type\" = 'utme';");

            migrationBuilder.CreateIndex(
                name: "IX_exam_questions_aloc_id_exam_type",
                table: "exam_questions",
                columns: new[] { "aloc_id", "exam_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exam_questions_aloc_id_exam_type",
                table: "exam_questions");

            migrationBuilder.DropColumn(
                name: "exam_type",
                table: "exam_questions");

            migrationBuilder.CreateIndex(
                name: "IX_exam_questions_aloc_id",
                table: "exam_questions",
                column: "aloc_id",
                unique: true);
        }
    }
}
