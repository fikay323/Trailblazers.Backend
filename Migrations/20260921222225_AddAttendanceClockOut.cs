using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trailblazers.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceClockOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "clock_out_accuracy_meters",
                table: "attendance_records",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "clock_out_distance_meters",
                table: "attendance_records",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "clock_out_latitude",
                table: "attendance_records",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "clock_out_longitude",
                table: "attendance_records",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "clock_out_time",
                table: "attendance_records",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "clock_out_accuracy_meters",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "clock_out_distance_meters",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "clock_out_latitude",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "clock_out_longitude",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "clock_out_time",
                table: "attendance_records");
        }
    }
}
