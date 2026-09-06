using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Features.Staff.Dtos;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class StaffManagementService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<StaffManagementService> logger) : IStaffManagementService
    {
        public async Task<List<StaffMemberDto>> GetStaffRosterAsync(CancellationToken cancellationToken = default)
        {
            var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
            var instructorUsers = await userManager.GetUsersInRoleAsync("Instructor");

            var staffList = new List<StaffMemberDto>();

            foreach (var a in adminUsers)
            {
                staffList.Add(new StaffMemberDto
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    Email = a.Email ?? string.Empty,
                    PhoneNumber = a.PhoneNumber,
                    Role = "Admin",
                    IsActive = a.IsActive,
                    Status = a.IsActive ? "Active" : "Suspended",
                    CreatedAt = a.CreatedAt
                });
            }

            foreach (var inst in instructorUsers)
            {
                // Avoid duplicates if a user somehow has both roles
                if (staffList.Any(s => s.Id == inst.Id)) continue;

                staffList.Add(new StaffMemberDto
                {
                    Id = inst.Id,
                    FullName = inst.FullName,
                    Email = inst.Email ?? string.Empty,
                    PhoneNumber = inst.PhoneNumber,
                    Role = "Instructor",
                    IsActive = inst.IsActive,
                    Status = inst.IsActive ? "Active" : "Suspended",
                    CreatedAt = inst.CreatedAt
                });
            }

            // Include pending (unaccepted) invitations
            var pendingInvites = await dbContext.StaffInvitations
                .Where(x => !x.IsAccepted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            foreach (var inv in pendingInvites)
            {
                // Only include if not already registered as active staff
                if (staffList.Any(s => string.Equals(s.Email, inv.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var isExpired = inv.ExpiresAt <= DateTimeOffset.UtcNow;
                staffList.Add(new StaffMemberDto
                {
                    Id = inv.Id,
                    FullName = inv.FullName,
                    Email = inv.Email,
                    Role = inv.Role,
                    IsActive = !isExpired,
                    Status = isExpired ? "ExpiredInvite" : "PendingInvite",
                    CreatedAt = inv.CreatedAt,
                    InvitationId = inv.Id,
                    InvitationExpiresAt = inv.ExpiresAt,
                    EmailDeliveryStatus = inv.EmailDeliveryStatus,
                    EmailDeliveryError = inv.EmailDeliveryError
                });
            }

            return staffList
                .OrderBy(s => s.Role == "Admin" ? 0 : 1)
                .ThenBy(s => s.FullName)
                .ToList();
        }

        public async Task<(bool Succeeded, string? Error, StaffMemberDto? StaffMember)> UpdateStaffRoleAsync(
            Guid staffId,
            string newRole,
            Guid requestedByUserId,
            CancellationToken cancellationToken = default)
        {
            var targetRole = newRole == "Admin" ? "Admin" : "Instructor";

            // First check if it's an invitation being updated
            var pendingInvite = await dbContext.StaffInvitations
                .FirstOrDefaultAsync(x => x.Id == staffId && !x.IsAccepted, cancellationToken);

            if (pendingInvite != null)
            {
                pendingInvite.Role = targetRole;
                pendingInvite.UpdatedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);

                return (true, null, new StaffMemberDto
                {
                    Id = pendingInvite.Id,
                    FullName = pendingInvite.FullName,
                    Email = pendingInvite.Email,
                    Role = pendingInvite.Role,
                    IsActive = pendingInvite.ExpiresAt > DateTimeOffset.UtcNow,
                    Status = "PendingInvite",
                    CreatedAt = pendingInvite.CreatedAt,
                    InvitationId = pendingInvite.Id,
                    InvitationExpiresAt = pendingInvite.ExpiresAt
                });
            }

            var user = await userManager.FindByIdAsync(staffId.ToString());
            if (user == null)
            {
                return (false, "Staff member not found.", null);
            }

            // Self-lockout protection: Cannot demote yourself if you are the sole administrator
            if (user.Id == requestedByUserId && targetRole != "Admin")
            {
                var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count <= 1)
                {
                    return (false, "Action blocked: You are the only active Administrator on the platform and cannot demote your own account.", null);
                }
            }

            var currentRoles = await userManager.GetRolesAsync(user);

            if (targetRole == "Admin")
            {
                if (currentRoles.Contains("Instructor"))
                {
                    await userManager.RemoveFromRoleAsync(user, "Instructor");
                }
                if (!currentRoles.Contains("Admin"))
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
            else
            {
                if (currentRoles.Contains("Admin"))
                {
                    await userManager.RemoveFromRoleAsync(user, "Admin");
                }
                if (!currentRoles.Contains("Instructor"))
                {
                    await userManager.AddToRoleAsync(user, "Instructor");
                }
            }

            logger.LogInformation("Staff member {Email} role changed to {Role} by user {AdminId}", user.Email, targetRole, requestedByUserId);

            return (true, null, new StaffMemberDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = targetRole,
                IsActive = user.IsActive,
                Status = user.IsActive ? "Active" : "Suspended",
                CreatedAt = user.CreatedAt
            });
        }

        public async Task<(bool Succeeded, string? Error)> ToggleStaffStatusAsync(
            Guid staffId,
            bool isActive,
            string? reason,
            Guid requestedByUserId,
            CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(staffId.ToString());
            if (user == null)
            {
                return (false, "Staff member not found.");
            }

            if (user.Id == requestedByUserId && !isActive)
            {
                return (false, "Action blocked: You cannot deactivate your own account.");
            }

            user.IsActive = isActive;
            user.DisabledReason = isActive ? null : (reason ?? "Account suspended by administrator.");
            var updateRes = await userManager.UpdateAsync(user);

            if (!updateRes.Succeeded)
            {
                return (false, string.Join("; ", updateRes.Errors.Select(e => e.Description)));
            }

            logger.LogInformation("Staff member {Email} status changed to {IsActive} by {AdminId}", user.Email, isActive, requestedByUserId);
            return (true, null);
        }
    }
}
