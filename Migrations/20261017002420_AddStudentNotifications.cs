using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trailblazers.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blog_posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Slug = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Excerpt = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CoverImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blog_posts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "student_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "General"),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_Category",
                table: "blog_posts",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_CreatedAt",
                table: "blog_posts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_IsPublished",
                table: "blog_posts",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_PublishedAt",
                table: "blog_posts",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_blog_posts_Slug",
                table: "blog_posts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_notifications_CreatedAt",
                table: "student_notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_student_notifications_IsRead",
                table: "student_notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_student_notifications_StudentEmail",
                table: "student_notifications",
                column: "StudentEmail");

            migrationBuilder.CreateIndex(
                name: "IX_student_notifications_StudentId",
                table: "student_notifications",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blog_posts");

            migrationBuilder.DropTable(
                name: "student_notifications");
        }
    }
}
