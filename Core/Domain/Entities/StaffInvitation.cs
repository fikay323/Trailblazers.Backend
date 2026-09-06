namespace Trailblazers.Backend.Core.Domain.Entities
{
    public class StaffInvitation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Instructor"; // "Instructor" or "Admin"
        public string TokenHash { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public Guid InvitedByUserId { get; set; }
        public string InvitedByUserName { get; set; } = string.Empty;
        public bool IsAccepted { get; set; } = false;
        public DateTimeOffset? AcceptedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
