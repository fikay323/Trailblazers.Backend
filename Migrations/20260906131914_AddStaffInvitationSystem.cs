using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trailblazers.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffInvitationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staff_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_invitations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_staff_invitations_Email",
                table: "staff_invitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_staff_invitations_IsAccepted",
                table: "staff_invitations",
                column: "IsAccepted");

            migrationBuilder.CreateIndex(
                name: "IX_staff_invitations_TokenHash",
                table: "staff_invitations",
                column: "TokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_invitations");
        }
    }
}
