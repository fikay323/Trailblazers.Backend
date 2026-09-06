namespace Trailblazers.Backend.Core.Application.Features.Staff.Dtos
{
    public class InviteStaffRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Instructor";
    }

    public class StaffMemberDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "Instructor";
        public bool IsActive { get; set; } = true;
        public string Status { get; set; } = "Active"; // "Active", "PendingInvite", "Suspended"
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? InvitationId { get; set; }
        public DateTimeOffset? InvitationExpiresAt { get; set; }
    }

    public class UpdateStaffRoleRequestDto
    {
        public string Role { get; set; } = "Instructor";
    }

    public class ToggleStaffStatusRequestDto
    {
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
    }

    public class ValidateInvitationResponseDto
    {
        public bool IsValid { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class AcceptInvitationRequestDto
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class StaffInvitationDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Instructor";
        public DateTimeOffset ExpiresAt { get; set; }
        public string InvitedByUserName { get; set; } = string.Empty;
        public bool IsAccepted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
