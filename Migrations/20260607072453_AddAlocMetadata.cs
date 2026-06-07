using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trailblazers.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAlocMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "aloc_id",
                table: "exam_questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "question_number",
                table: "exam_questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_exam_questions_aloc_id",
                table: "exam_questions",
                column: "aloc_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exam_questions_aloc_id",
                table: "exam_questions");

            migrationBuilder.DropColumn(
                name: "aloc_id",
                table: "exam_questions");

            migrationBuilder.DropColumn(
                name: "question_number",
                table: "exam_questions");
        }
    }
}
