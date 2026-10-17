using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/students")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public class AdminStudentsController(
        UserManager<ApplicationUser> userManager,
        IExamSessionRepository sessionRepository,
        IImageStorageService imageStorageService,
        ApplicationDbContext dbContext,
        IMailService mailService,
        IEmailTemplateService emailTemplateService,
        IConfiguration configuration,
        ILogger<AdminStudentsController> logger) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAllStudents(CancellationToken cancellationToken)
        {
            var students = await userManager.GetUsersInRoleAsync("Student");
            var allResults = (await sessionRepository.GetAllResultsAsync(cancellationToken)).ToList();

            var studentDtos = students.Select(s =>
            {
                var sEmail = (s.Email ?? string.Empty).Trim().ToLowerInvariant();
                var studentResults = allResults.Where(r => r.CandidateId == sEmail).ToList();
                var testsCount = studentResults.Count;

                double avgScore = 0;
                if (testsCount > 0)
                {
                    avgScore = Math.Round(studentResults.Average(r =>
                    {
                        var totalQ = r.Session?.AssignedQuestionIds?.Count ?? 50;
                        return totalQ > 0 ? (double)r.TotalScore / totalQ * 100 : 0;
                    }), 1);
                }

                return new AdminStudentListItemDto
                {
                    Id = s.Id,
                    FullName = s.FullName,
                    Email = s.Email ?? string.Empty,
                    PhoneNumber = s.PhoneNumber,
                    IsActive = s.IsActive,
                    DisabledReason = s.DisabledReason,
                    ProfilePictureUrl = s.ProfilePictureUrl,
                    TotalTestsTaken = testsCount,
                    AverageScore = avgScore,
                    CreatedAt = s.CreatedAt
                };
            }).OrderByDescending(s => s.CreatedAt).ToList();

            return Ok(studentDtos);
        }

        [HttpPost("{id:guid}/status")]
        public async Task<IActionResult> ToggleStudentStatus(
            Guid id,
            [FromBody] UpdateStudentStatusRequestDto request)
        {
            if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Admin"))
            {
                return StatusCode(403, new { error = "Access denied: Only Administrators can modify student account fee status." });
            }

            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { error = "Student not found." });
            }

            user.IsActive = request.IsActive;
            user.DisabledReason = request.IsActive ? null : (request.Reason?.Trim() ?? "Account deactivated by administration.");

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                return BadRequest(new { error = errors });
            }

            logger.LogInformation("Admin updated student {Email} status: IsActive={IsActive}, Reason={Reason}",
                user.Email, user.IsActive, user.DisabledReason);

            return Ok(new
            {
                message = user.IsActive ? "Student account activated successfully." : "Student account deactivated.",
                user.Id,
                user.Email,
                user.IsActive,
                user.DisabledReason
            });
        }

        [HttpGet("{email}/history")]
        public async Task<IActionResult> GetStudentHistory(string email, CancellationToken cancellationToken)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var results = (await sessionRepository.GetResultsByStudentEmailAsync(normalizedEmail, cancellationToken)).ToList();

            var attempts = results.Select(r =>
            {
                var totalQuestions = (r.Session?.AssignedQuestionIds != null && r.Session.AssignedQuestionIds.Count > 0)
                    ? r.Session.AssignedQuestionIds.Count
                    : (r.Session?.Answers?.Count ?? 0);

                var percentage = totalQuestions > 0
                    ? Math.Round((double)r.TotalScore / totalQuestions * 100, 1)
                    : 0;

                return new
                {
                    SessionId = r.SessionId,
                    TargetYear = r.Session?.TargetYear ?? 0,
                    TotalScore = r.TotalScore,
                    TotalQuestions = totalQuestions,
                    Percentage = percentage,
                    CompletedAt = r.CompletedAt,
                    StartTime = r.Session?.StartTime ?? r.CompletedAt
                };
            }).ToList();

            return Ok(new
            {
                StudentEmail = normalizedEmail,
                TotalTestsTaken = attempts.Count,
                AveragePercentage = attempts.Count > 0 ? Math.Round(attempts.Average(a => a.Percentage), 1) : 0,
                HighestPercentage = attempts.Count > 0 ? attempts.Max(a => a.Percentage) : 0,
                Attempts = attempts
            });
        }

        [HttpDelete("{id:guid}/profile-picture")]
        public async Task<IActionResult> DeleteStudentProfilePicture(
            Guid id,
            [FromBody] DeleteProfilePictureByAdminRequestDto request,
            CancellationToken cancellationToken)
        {
            var student = await userManager.FindByIdAsync(id.ToString());
            if (student == null)
            {
                return NotFound(new { error = "Student not found." });
            }

            var reason = string.IsNullOrWhiteSpace(request?.Reason)
                ? "Profile picture did not comply with academy standards."
                : request.Reason.Trim();

            // Clean up Cloudinary asset if present
            if (!string.IsNullOrWhiteSpace(student.ProfilePicturePublicId))
            {
                logger.LogInformation("Admin deleting Cloudinary asset {PublicId} for student {StudentId}",
                    student.ProfilePicturePublicId, student.Id);
                await imageStorageService.DeleteImageAsync(student.ProfilePicturePublicId, cancellationToken);
            }

            student.ProfilePictureUrl = null;
            student.ProfilePicturePublicId = null;

            var updateResult = await userManager.UpdateAsync(student);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = errors });
            }

            var actorName = User.FindFirstValue("fullName")
                            ?? User.FindFirstValue(ClaimTypes.Name)
                            ?? "Academy Administration";

            // Create in-app student notification
            var notification = new StudentNotification
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                StudentEmail = student.Email ?? string.Empty,
                Title = "Profile Picture Removed",
                Message = $"Your profile picture was removed by {actorName}. Reason: {reason}",
                Type = "ProfilePictureRemoved",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = actorName
            };

            dbContext.StudentNotifications.Add(notification);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Dispatch notification email to student
            try
            {
                var portalUrl = configuration["Frontend:StudentPortalUrl"]
                                ?? "https://learn.trailblazer.com/student/dashboard";

                var emailHtml = emailTemplateService.RenderProfilePictureRemovedEmail(
                    student.FullName,
                    reason,
                    portalUrl);

                await mailService.SendEmailAsync(
                    student.Email!,
                    "Profile Picture Removal Notice - Trailblazers Academy",
                    emailHtml,
                    isHtml: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send profile picture removal email to {Email}", student.Email);
            }

            logger.LogInformation("Admin/Staff successfully removed profile picture for student {Email} with reason: {Reason}",
                student.Email, reason);

            return Ok(new
            {
                message = "Student profile picture removed successfully and notification dispatched.",
                studentId = student.Id,
                reason
            });
        }
    }

    public class AdminStudentListItemDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public string? DisabledReason { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public int TotalTestsTaken { get; set; }
        public double AverageScore { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class UpdateStudentStatusRequestDto
    {
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
    }

    public class DeleteProfilePictureByAdminRequestDto
    {
        public string? Reason { get; set; }
    }
}
