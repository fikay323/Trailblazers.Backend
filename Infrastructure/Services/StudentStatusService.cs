using Microsoft.AspNetCore.Identity;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class StudentStatusService(UserManager<ApplicationUser> userManager) : IStudentStatusService
    {
        public async Task<(bool IsAllowed, string? Reason)> ValidateStudentAccessAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return (true, null);

            var user = await userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());
            if (user != null && !user.IsActive)
            {
                return (false, user.DisabledReason ?? "Your student account has been deactivated by administration.");
            }

            return (true, null);
        }
    }
}
