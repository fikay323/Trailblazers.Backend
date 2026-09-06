using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trailblazers.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendance_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    clock_in_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    accuracy_meters = table.Column<double>(type: "double precision", nullable: true),
                    distance_meters = table.Column<double>(type: "double precision", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    verification_type = table.Column<int>(type: "integer", nullable: false),
                    marked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    marked_by_user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_attendance_records_AspNetUsers_student_id",
                        column: x => x.student_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attendance_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    center_latitude = table.Column<double>(type: "double precision", nullable: false),
                    center_longitude = table.Column<double>(type: "double precision", nullable: false),
                    allowed_radius_meters = table.Column<double>(type: "double precision", nullable: false),
                    max_allowed_accuracy_meters = table.Column<double>(type: "double precision", nullable: false),
                    earliest_clock_in_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    late_cutoff_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    latest_clock_in_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_date",
                table: "attendance_records",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_student_date",
                table: "attendance_records",
                columns: new[] { "student_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_records");

            migrationBuilder.DropTable(
                name: "attendance_settings");
        }
    }
}
