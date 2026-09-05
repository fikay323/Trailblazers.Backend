using Microsoft.AspNetCore.Identity;

namespace Trailblazers.Backend.Core.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? DisabledReason { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public List<RefreshToken> RefreshTokens { get; set; } = [];
    }
}
