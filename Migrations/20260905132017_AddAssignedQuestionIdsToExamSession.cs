using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trailblazers.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedQuestionIdsToExamSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "assigned_question_ids",
                table: "exam_sessions",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::uuid[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "assigned_question_ids",
                table: "exam_sessions");
        }
    }
}
