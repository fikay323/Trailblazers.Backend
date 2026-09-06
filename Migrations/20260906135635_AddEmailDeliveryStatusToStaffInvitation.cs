using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trailblazers.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryStatusToStaffInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailDeliveryError",
                table: "staff_invitations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailDeliveryStatus",
                table: "staff_invitations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailDeliveryError",
                table: "staff_invitations");

            migrationBuilder.DropColumn(
                name: "EmailDeliveryStatus",
                table: "staff_invitations");
        }
    }
}
