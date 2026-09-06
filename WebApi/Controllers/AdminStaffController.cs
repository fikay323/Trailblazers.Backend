using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Features.Staff.Dtos;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/staff")]
    public class AdminStaffController(
        IStaffInvitationService invitationService,
        IStaffManagementService staffManagementService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ILogger<AdminStaffController> logger) : ControllerBase
    {
        [HttpGet]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> GetStaffRoster(CancellationToken cancellationToken)
        {
            try
            {
                var roster = await staffManagementService.GetStaffRosterAsync(cancellationToken);
                return Ok(roster);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch staff roster");
                return StatusCode(500, new { error = "An internal error occurred while fetching staff roster." });
            }
        }

        [HttpPost("invite")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> InviteStaff(
            [FromBody] InviteStaffRequestDto request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { error = "Staff email address is required." });
            }

            var (callerId, callerName) = await ResolveCurrentStaffUserAsync();

            try
            {
                var invitation = await invitationService.InviteStaffAsync(
                    request,
                    callerId,
                    callerName,
                    cancellationToken);

                return Ok(invitation);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch staff invitation for {Email}", request.Email);
                return StatusCode(500, new { error = "An internal error occurred while sending staff invitation." });
            }
        }

        [HttpPost("invitations/{id:guid}/resend")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> ResendInvitation(Guid id, CancellationToken cancellationToken)
        {
            var (callerId, _) = await ResolveCurrentStaffUserAsync();

            try
            {
                var result = await invitationService.ResendInvitationAsync(id, callerId, cancellationToken);
                if (!result.Succeeded)
                {
                    return NotFound(new { error = result.Error ?? "Invitation not found or has already been accepted." });
                }

                return Ok(new
                {
                    message = result.EmailStatusMessage ?? "Invitation refreshed successfully.",
                    inviteUrl = result.InviteUrl,
                    emailSent = result.EmailSent,
                    emailStatusMessage = result.EmailStatusMessage
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resend staff invitation {Id}", id);
                return StatusCode(500, new { error = "Failed to resend invitation email." });
            }
        }

        [HttpDelete("invitations/{id:guid}")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> DeleteInvitation(Guid id, CancellationToken cancellationToken)
        {
            var (callerId, _) = await ResolveCurrentStaffUserAsync();

            try
            {
                var success = await invitationService.DeleteInvitationAsync(id, callerId, cancellationToken);
                if (!success)
                {
                    return NotFound(new { error = "Invitation not found or has already been accepted." });
                }

                return Ok(new { message = "Staff invitation deleted successfully." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete staff invitation {Id}", id);
                return StatusCode(500, new { error = "Failed to delete staff invitation." });
            }
        }

        [HttpPut("{id:guid}/role")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> UpdateStaffRole(
            Guid id,
            [FromBody] UpdateStaffRoleRequestDto request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Role) || (request.Role != "Admin" && request.Role != "Instructor"))
            {
                return BadRequest(new { error = "Role must be either 'Admin' or 'Instructor'." });
            }

            var (callerId, _) = await ResolveCurrentStaffUserAsync();

            try
            {
                var (succeeded, error, updatedStaff) = await staffManagementService.UpdateStaffRoleAsync(
                    id,
                    request.Role,
                    callerId,
                    cancellationToken);

                if (!succeeded)
                {
                    return BadRequest(new { error });
                }

                return Ok(updatedStaff);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update role for staff {Id}", id);
                return StatusCode(500, new { error = "An internal error occurred while updating staff role." });
            }
        }

        [HttpPut("{id:guid}/status")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> ToggleStaffStatus(
            Guid id,
            [FromBody] ToggleStaffStatusRequestDto request,
            CancellationToken cancellationToken)
        {
            var (callerId, _) = await ResolveCurrentStaffUserAsync();

            try
            {
                var (succeeded, error) = await staffManagementService.ToggleStaffStatusAsync(
                    id,
                    request.IsActive,
                    request.Reason,
                    callerId,
                    cancellationToken);

                if (!succeeded)
                {
                    return BadRequest(new { error });
                }

                return Ok(new { message = $"Staff account status successfully updated to {(request.IsActive ? "Active" : "Suspended")}." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to toggle status for staff {Id}", id);
                return StatusCode(500, new { error = "An internal error occurred while updating staff status." });
            }
        }

        [HttpGet("invitations/validate")]
        public async Task<IActionResult> ValidateInvitation(
            [FromQuery] string token,
            [FromQuery] string email,
            CancellationToken cancellationToken)
        {
            var result = await invitationService.ValidateInvitationAsync(token, email, cancellationToken);
            if (!result.IsValid)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("invitations/accept")]
        public async Task<IActionResult> AcceptInvitation(
            [FromBody] AcceptInvitationRequestDto request,
            CancellationToken cancellationToken)
        {
            var (succeeded, error, authResult) = await invitationService.AcceptInvitationAsync(request, cancellationToken);
            if (!succeeded || authResult == null)
            {
                return BadRequest(new { error = error ?? "Failed to accept invitation." });
            }

            AppendAuthCookies(authResult.Token, authResult.RefreshToken);

            return Ok(authResult);
        }

        private async Task<(Guid UserId, string FullName)> ResolveCurrentStaffUserAsync()
        {
            var emailClaim = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (!string.IsNullOrWhiteSpace(emailClaim))
            {
                var user = await userManager.FindByEmailAsync(emailClaim);
                if (user != null)
                {
                    return (user.Id, user.FullName);
                }
            }

            // Fallback for API Key callers
            return (Guid.Empty, "System Administrator");
        }

        private void AppendAuthCookies(string token, string refreshToken)
        {
            var cookieDomain = Environment.GetEnvironmentVariable("COOKIE_DOMAIN");
            var isSecure = !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

            var authCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            };

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };

            if (!string.IsNullOrWhiteSpace(cookieDomain))
            {
                authCookieOptions.Domain = cookieDomain;
                refreshCookieOptions.Domain = cookieDomain;
            }

            Response.Cookies.Append("auth_token", token, authCookieOptions);
            Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOptions);
        }

        [HttpPost("maintenance/purge-dev-database")]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        public async Task<IActionResult> PurgeDevDatabase(CancellationToken cancellationToken)
        {
            try
            {
                logger.LogWarning("Dev database purge initiated by authorized administrator or API key.");

                // Identify Admin user(s) to preserve
                var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
                var adminUserIds = adminUsers.Select(u => u.Id).ToHashSet();

                var defaultAdmin = await userManager.FindByEmailAsync("admin@trailblazer.edu");
                if (defaultAdmin != null)
                {
                    adminUserIds.Add(defaultAdmin.Id);
                }

                if (adminUserIds.Count == 0)
                {
                    return BadRequest(new { error = "Cannot purge database: No admin account was found to preserve. Please ensure at least one Admin user exists." });
                }

                var adminIdsList = adminUserIds.ToList();

                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                // 1. Delete all exam results and sessions
                var deletedResults = await dbContext.ExamResults.ExecuteDeleteAsync(cancellationToken);
                var deletedSessions = await dbContext.ExamSessions.ExecuteDeleteAsync(cancellationToken);

                // 2. Delete all attendance clock-in records
                var deletedAttendance = await dbContext.AttendanceRecords.ExecuteDeleteAsync(cancellationToken);

                // 3. Delete all active refresh tokens
                var deletedTokens = await dbContext.RefreshTokens.ExecuteDeleteAsync(cancellationToken);

                // 4. Delete all staff invitations
                var deletedInvitations = await dbContext.StaffInvitations.ExecuteDeleteAsync(cancellationToken);

                // 5. Delete all student registrations and contact inquiries
                var deletedSubmissions = await dbContext.Submissions.ExecuteDeleteAsync(cancellationToken);

                // 6. Delete Identity Child Tables for Non-Admin Users
                await dbContext.UserRoles.Where(ur => !adminUserIds.Contains(ur.UserId)).ExecuteDeleteAsync(cancellationToken);
                await dbContext.UserClaims.Where(uc => !adminUserIds.Contains(uc.UserId)).ExecuteDeleteAsync(cancellationToken);
                await dbContext.UserLogins.Where(ul => !adminUserIds.Contains(ul.UserId)).ExecuteDeleteAsync(cancellationToken);
                await dbContext.UserTokens.Where(ut => !adminUserIds.Contains(ut.UserId)).ExecuteDeleteAsync(cancellationToken);

                // 7. Delete non-admin users from Users
                var deletedUsers = await dbContext.Users.Where(u => !adminUserIds.Contains(u.Id)).ExecuteDeleteAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                var preservedQuestionsCount = await dbContext.ExamQuestions.CountAsync(cancellationToken);
                var remainingUsersCount = await dbContext.Users.CountAsync(cancellationToken);

                logger.LogInformation("Dev database purge complete. Preserved {Questions} questions and {Users} admin user(s).",
                    preservedQuestionsCount, remainingUsersCount);

                return Ok(new
                {
                    message = "Dev stage database successfully purged. All test records cleared while preserving exam questions and the admin account.",
                    preservedExamQuestions = preservedQuestionsCount,
                    preservedAdminUsers = remainingUsersCount,
                    deletedRecords = new
                    {
                        examResults = deletedResults,
                        examSessions = deletedSessions,
                        attendanceRecords = deletedAttendance,
                        refreshTokens = deletedTokens,
                        staffInvitations = deletedInvitations,
                        submissions = deletedSubmissions,
                        users = deletedUsers
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute database purge");
                return StatusCode(500, new { error = $"Database purge failed: {ex.Message}" });
            }
        }
    }
}
