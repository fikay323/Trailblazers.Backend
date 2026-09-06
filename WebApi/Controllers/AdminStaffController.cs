using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Trailblazers.Backend.Core.Application.Features.Staff.Dtos;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/staff")]
    public class AdminStaffController(
        IStaffInvitationService invitationService,
        IStaffManagementService staffManagementService,
        UserManager<ApplicationUser> userManager,
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
                var success = await invitationService.ResendInvitationAsync(id, callerId, cancellationToken);
                if (!success)
                {
                    return NotFound(new { error = "Invitation not found or has already been accepted." });
                }

                return Ok(new { message = "Invitation email resent successfully." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resend staff invitation {Id}", id);
                return StatusCode(500, new { error = "Failed to resend invitation email." });
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
    }
}
