using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Features.Staff.Dtos;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class StaffInvitationService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmailTemplateService templateService,
        IMailService mailService,
        IJwtTokenService jwtTokenService,
        ILogger<StaffInvitationService> logger) : IStaffInvitationService
    {
        public async Task<StaffInvitationDto> InviteStaffAsync(
            InviteStaffRequestDto request,
            Guid invitedByUserId,
            string invitedByUserName,
            CancellationToken cancellationToken = default)
        {
            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            var cleanName = string.IsNullOrWhiteSpace(request.FullName) ? "Colleague" : request.FullName.Trim();
            var cleanRole = request.Role == "Admin" ? "Admin" : "Instructor";

            // Verify if user already exists with an active staff role
            var existingUser = await userManager.FindByEmailAsync(cleanEmail);
            if (existingUser != null)
            {
                var existingRoles = await userManager.GetRolesAsync(existingUser);
                if (existingRoles.Contains("Admin") || existingRoles.Contains("Instructor"))
                {
                    throw new InvalidOperationException($"An active staff account with email '{cleanEmail}' already exists ({string.Join(", ", existingRoles)}).");
                }
            }

            // Invalidate any prior unaccepted invitations for this email
            var pendingInvites = await dbContext.StaffInvitations
                .Where(x => x.Email == cleanEmail && !x.IsAccepted)
                .ToListAsync(cancellationToken);

            foreach (var p in pendingInvites)
            {
                p.ExpiresAt = DateTimeOffset.UtcNow; // Expire old tokens
            }

            // Generate secure random invitation token
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var tokenHash = HashToken(rawToken);

            var invitation = new StaffInvitation
            {
                Id = Guid.NewGuid(),
                Email = cleanEmail,
                FullName = cleanName,
                Role = cleanRole,
                TokenHash = tokenHash,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(48),
                InvitedByUserId = invitedByUserId,
                InvitedByUserName = invitedByUserName,
                IsAccepted = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.StaffInvitations.Add(invitation);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Construct invitation link
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                           ?? Environment.GetEnvironmentVariable("APP_URL")
                           ?? "http://localhost:3000";

            var inviteUrl = $"{frontendUrl.TrimEnd('/')}/auth/accept-invite?token={rawToken}&email={Uri.EscapeDataString(cleanEmail)}";

            bool emailSent = false;
            string? emailStatusMessage = null;

            try
            {
                var emailHtml = templateService.RenderStaffInvitationEmail(
                    recipientName: cleanName,
                    role: cleanRole,
                    inviteUrl: inviteUrl,
                    invitedByName: invitedByUserName);

                await mailService.SendEmailAsync(
                    to: cleanEmail,
                    subject: $"Invitation: Join the Trailblazers Academy Staff as {cleanRole}",
                    body: emailHtml,
                    isHtml: true);

                emailSent = true;
                emailStatusMessage = $"Invitation email successfully sent to {cleanEmail}.";
                invitation.EmailDeliveryStatus = "Sent";
                invitation.EmailDeliveryError = null;
                logger.LogInformation("Staff invitation email successfully dispatched to {Email} ({Role})", cleanEmail, cleanRole);
            }
            catch (Exception ex)
            {
                emailSent = false;
                var reason = ex is TimeoutException ? "Connection timed out (outbound SMTP ports may be blocked on Render)" : ex.Message;
                emailStatusMessage = $"Email delivery failed: {reason}. You can copy and share the invitation link directly.";
                invitation.EmailDeliveryStatus = "Failed";
                invitation.EmailDeliveryError = reason;
                logger.LogWarning(ex, "Could not send staff invitation email to {Email}: {Reason}", cleanEmail, reason);
            }

            // Save status update
            await dbContext.SaveChangesAsync(cancellationToken);

            return new StaffInvitationDto
            {
                Id = invitation.Id,
                Email = invitation.Email,
                FullName = invitation.FullName,
                Role = invitation.Role,
                ExpiresAt = invitation.ExpiresAt,
                InvitedByUserName = invitation.InvitedByUserName,
                IsAccepted = invitation.IsAccepted,
                CreatedAt = invitation.CreatedAt,
                InviteUrl = inviteUrl,
                EmailSent = emailSent,
                EmailStatusMessage = emailStatusMessage
            };
        }

        public async Task<ValidateInvitationResponseDto> ValidateInvitationAsync(
            string token,
            string email,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                return new ValidateInvitationResponseDto { IsValid = false, ErrorMessage = "Invalid invitation link." };
            }

            var cleanEmail = email.Trim().ToLowerInvariant();
            var tokenHash = HashToken(token.Trim());

            var invitation = await dbContext.StaffInvitations
                .FirstOrDefaultAsync(x => x.Email == cleanEmail && x.TokenHash == tokenHash, cancellationToken);

            if (invitation == null)
            {
                return new ValidateInvitationResponseDto { IsValid = false, ErrorMessage = "Invitation not found or link has expired." };
            }

            if (invitation.IsAccepted)
            {
                return new ValidateInvitationResponseDto { IsValid = false, ErrorMessage = "This invitation has already been accepted. Please sign in." };
            }

            if (invitation.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return new ValidateInvitationResponseDto { IsValid = false, ErrorMessage = "This invitation link has expired. Please ask an administrator to resend your invite." };
            }

            return new ValidateInvitationResponseDto
            {
                IsValid = true,
                FullName = invitation.FullName,
                Email = invitation.Email,
                Role = invitation.Role
            };
        }

        public async Task<(bool Succeeded, string? Error, StaffAuthResultDto? AuthResult)> AcceptInvitationAsync(
            AcceptInvitationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return (false, "Password must be at least 6 characters long.", null);
            }

            var validation = await ValidateInvitationAsync(request.Token, request.Email, cancellationToken);
            if (!validation.IsValid)
            {
                return (false, validation.ErrorMessage, null);
            }

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            var tokenHash = HashToken(request.Token.Trim());

            var invitation = await dbContext.StaffInvitations
                .FirstAsync(x => x.Email == cleanEmail && x.TokenHash == tokenHash, cancellationToken);

            var user = await userManager.FindByEmailAsync(cleanEmail);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = cleanEmail,
                    Email = cleanEmail,
                    FullName = invitation.FullName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createRes = await userManager.CreateAsync(user, request.Password);
                if (!createRes.Succeeded)
                {
                    var errors = string.Join("; ", createRes.Errors.Select(e => e.Description));
                    return (false, errors, null);
                }

                await userManager.AddToRoleAsync(user, invitation.Role);
            }
            else
            {
                // Existing account (e.g. promoting user): reset password to chosen password
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetRes = await userManager.ResetPasswordAsync(user, resetToken, request.Password);
                if (!resetRes.Succeeded)
                {
                    var errors = string.Join("; ", resetRes.Errors.Select(e => e.Description));
                    return (false, errors, null);
                }

                user.EmailConfirmed = true;
                user.IsActive = true;
                user.FullName = invitation.FullName;
                await userManager.UpdateAsync(user);

                var currentRoles = await userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(invitation.Role))
                {
                    await userManager.AddToRoleAsync(user, invitation.Role);
                }
            }

            // Mark invitation accepted
            invitation.IsAccepted = true;
            invitation.AcceptedAt = DateTimeOffset.UtcNow;
            invitation.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);

            // Generate JWT authentication tokens
            var roles = await userManager.GetRolesAsync(user);
            var token = jwtTokenService.GenerateAccessToken(user, roles);
            var refreshToken = jwtTokenService.GenerateRefreshToken(user.Id);

            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Staff invitation accepted by {Email}. Assigned role: {Role}", cleanEmail, invitation.Role);

            return (true, null, new StaffAuthResultDto
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                UserId = user.Id,
                Email = user.Email ?? cleanEmail,
                FullName = user.FullName,
                Role = invitation.Role
            });
        }

        public async Task<ResendInvitationResponseDto> ResendInvitationAsync(
            Guid invitationId,
            Guid requestedByUserId,
            CancellationToken cancellationToken = default)
        {
            var invitation = await dbContext.StaffInvitations
                .FirstOrDefaultAsync(x => x.Id == invitationId, cancellationToken);

            if (invitation == null || invitation.IsAccepted)
            {
                return new ResendInvitationResponseDto
                {
                    Succeeded = false,
                    Error = "Invitation not found or has already been accepted."
                };
            }

            // Generate fresh token and extend validity by 48 hours
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            invitation.TokenHash = HashToken(rawToken);
            invitation.ExpiresAt = DateTimeOffset.UtcNow.AddHours(48);
            invitation.UpdatedAt = DateTimeOffset.UtcNow;

            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                           ?? Environment.GetEnvironmentVariable("APP_URL")
                           ?? "http://localhost:3000";

            var inviteUrl = $"{frontendUrl.TrimEnd('/')}/auth/accept-invite?token={rawToken}&email={Uri.EscapeDataString(invitation.Email)}";

            bool emailSent = false;
            string? emailStatusMessage = null;

            try
            {
                var emailHtml = templateService.RenderStaffInvitationEmail(
                    recipientName: invitation.FullName,
                    role: invitation.Role,
                    inviteUrl: inviteUrl,
                    invitedByName: invitation.InvitedByUserName);

                await mailService.SendEmailAsync(
                    to: invitation.Email,
                    subject: $"Reminder: Join the Trailblazers Academy Staff as {invitation.Role}",
                    body: emailHtml,
                    isHtml: true);

                emailSent = true;
                emailStatusMessage = $"Invitation email successfully resent to {invitation.Email}.";
                invitation.EmailDeliveryStatus = "Sent";
                invitation.EmailDeliveryError = null;
                logger.LogInformation("Staff invitation resent to {Email}", invitation.Email);
            }
            catch (Exception ex)
            {
                emailSent = false;
                var reason = ex is TimeoutException ? "Connection timed out (outbound SMTP ports may be blocked on Render)" : ex.Message;
                emailStatusMessage = $"Email delivery failed: {reason}. You can copy and share the invitation link directly.";
                invitation.EmailDeliveryStatus = "Failed";
                invitation.EmailDeliveryError = reason;
                logger.LogWarning(ex, "Failed to resend staff invitation email to {Email}: {Reason}", invitation.Email, reason);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return new ResendInvitationResponseDto
            {
                Succeeded = true,
                InviteUrl = inviteUrl,
                EmailSent = emailSent,
                EmailStatusMessage = emailStatusMessage
            };
        }

        public async Task<bool> DeleteInvitationAsync(
            Guid invitationId,
            Guid requestedByUserId,
            CancellationToken cancellationToken = default)
        {
            var invitation = await dbContext.StaffInvitations
                .FirstOrDefaultAsync(x => x.Id == invitationId && !x.IsAccepted, cancellationToken);

            if (invitation == null)
            {
                return false;
            }

            dbContext.StaffInvitations.Remove(invitation);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Staff invitation {Id} for {Email} deleted by User {UserId}",
                invitationId, invitation.Email, requestedByUserId);

            return true;
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
