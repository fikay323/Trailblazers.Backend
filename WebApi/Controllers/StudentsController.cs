using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController(
        IExamSessionRepository sessionRepository,
        UserManager<ApplicationUser> userManager,
        IImageStorageService imageStorageService,
        ApplicationDbContext dbContext,
        ILogger<StudentsController> logger) : ControllerBase
    {
        [HttpGet("me/exam-history")]
        public async Task<IActionResult> GetMyExamHistory(
            [FromQuery] string? email,
            CancellationToken cancellationToken)
        {
            // Determine email from JWT claim or query parameter
            var userEmail = User.FindFirstValue(ClaimTypes.Email)
                            ?? User.FindFirstValue("email")
                            ?? email;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return BadRequest(new { error = "Student email is required." });
            }

            var results = (await sessionRepository.GetResultsByStudentEmailAsync(userEmail, cancellationToken)).ToList();

            var attempts = results.Select(r =>
            {
                var totalQuestions = (r.Session?.AssignedQuestionIds != null && r.Session.AssignedQuestionIds.Count > 0)
                    ? r.Session.AssignedQuestionIds.Count
                    : (r.Session?.Answers?.Count ?? 0);

                var percentage = totalQuestions > 0
                    ? Math.Round((double)r.TotalScore / totalQuestions * 100, 1)
                    : 0;

                return new StudentExamAttemptDto
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

            var totalTests = attempts.Count;
            var averageScore = totalTests > 0
                ? Math.Round(attempts.Average(a => a.Percentage), 1)
                : 0;
            var highestScore = totalTests > 0
                ? attempts.Max(a => a.Percentage)
                : 0;

            return Ok(new StudentHistoryResponseDto
            {
                StudentEmail = userEmail,
                TotalTestsTaken = totalTests,
                AveragePercentage = averageScore,
                HighestPercentage = highestScore,
                Attempts = attempts
            });
        }

        [HttpGet("me/profile")]
        public async Task<IActionResult> GetMyProfile([FromQuery] string? email)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email)
                            ?? User.FindFirstValue("email")
                            ?? email;

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return BadRequest(new { error = "Student email is required." });
            }

            var user = await userManager.FindByEmailAsync(userEmail.Trim().ToLowerInvariant());
            if (user == null)
            {
                return NotFound(new { error = "Student account not found." });
            }

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.IsActive,
                user.DisabledReason,
                user.CreatedAt,
                user.ProfilePictureUrl
            });
        }

        [Authorize]
        [HttpPost("me/profile-picture")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadProfilePicture(
            [FromForm] IFormFile file,
            [FromQuery] string? email,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No image file was provided." });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { error = "Image file size exceeds the 5MB limit." });
            }

            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/jpg" };
            if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(new { error = "Only JPEG, PNG, and WebP images are allowed." });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue("sub")
                               ?? User.FindFirstValue("id");

            ApplicationUser? user = null;
            if (!string.IsNullOrWhiteSpace(userIdString) && Guid.TryParse(userIdString, out var userGuid))
            {
                user = await userManager.FindByIdAsync(userGuid.ToString());
            }

            if (user == null)
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email)
                                ?? User.FindFirstValue("email")
                                ?? email;

                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    user = await userManager.FindByEmailAsync(userEmail.Trim().ToLowerInvariant());
                }
            }

            if (user == null)
            {
                return Unauthorized(new { error = "Student account could not be found." });
            }

            // Clean up old Cloudinary asset if present to prevent wasted storage
            if (!string.IsNullOrWhiteSpace(user.ProfilePicturePublicId))
            {
                logger.LogInformation("Deleting existing Cloudinary asset {PublicId} for user {UserId}",
                    user.ProfilePicturePublicId, user.Id);
                await imageStorageService.DeleteImageAsync(user.ProfilePicturePublicId, cancellationToken);
            }

            // Upload new cropped image stream to Cloudinary
            await using var stream = file.OpenReadStream();
            var (url, publicId) = await imageStorageService.UploadProfilePictureAsync(
                stream,
                file.FileName,
                user.Id.ToString(),
                cancellationToken);

            user.ProfilePictureUrl = url;
            user.ProfilePicturePublicId = publicId;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                logger.LogError("Failed to update user profile picture in database: {Errors}",
                    string.Join("; ", updateResult.Errors.Select(e => e.Description)));
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to update profile picture in database." });
            }

            return Ok(new
            {
                message = "Profile picture uploaded successfully.",
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        [Authorize]
        [HttpDelete("me/profile-picture")]
        public async Task<IActionResult> DeleteProfilePicture(
            [FromQuery] string? email,
            CancellationToken cancellationToken)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? User.FindFirstValue("sub")
                               ?? User.FindFirstValue("id");

            ApplicationUser? user = null;
            if (!string.IsNullOrWhiteSpace(userIdString) && Guid.TryParse(userIdString, out var userGuid))
            {
                user = await userManager.FindByIdAsync(userGuid.ToString());
            }

            if (user == null)
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email)
                                ?? User.FindFirstValue("email")
                                ?? email;

                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    user = await userManager.FindByEmailAsync(userEmail.Trim().ToLowerInvariant());
                }
            }

            if (user == null)
            {
                return Unauthorized(new { error = "Student account could not be found." });
            }

            if (string.IsNullOrWhiteSpace(user.ProfilePicturePublicId) && string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
            {
                return Ok(new { message = "No profile picture to delete." });
            }

            if (!string.IsNullOrWhiteSpace(user.ProfilePicturePublicId))
            {
                logger.LogInformation("Destroying Cloudinary asset {PublicId} for user {UserId}",
                    user.ProfilePicturePublicId, user.Id);
                await imageStorageService.DeleteImageAsync(user.ProfilePicturePublicId, cancellationToken);
            }

            user.ProfilePictureUrl = null;
            user.ProfilePicturePublicId = null;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                logger.LogError("Failed to clear profile picture in database: {Errors}",
                    string.Join("; ", updateResult.Errors.Select(e => e.Description)));
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to clear profile picture in database." });
            }

            return Ok(new { message = "Profile picture deleted successfully." });
        }

        [HttpGet("me/notifications")]
        public async Task<IActionResult> GetMyNotifications(
            [FromQuery] string? email,
            CancellationToken cancellationToken)
        {
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email)
                                ?? User.FindFirstValue("email")
                                ?? email;

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return BadRequest(new { error = "Student email is required." });
                }

                var normalizedEmail = userEmail.Trim().ToLowerInvariant();

                var notifications = await dbContext.StudentNotifications
                    .Where(n => n.StudentEmail.ToLower() == normalizedEmail)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(50)
                    .Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Message,
                        n.Type,
                        n.IsRead,
                        n.CreatedAt,
                        n.CreatedBy
                    })
                    .ToListAsync(cancellationToken);

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve student notifications for {Email}", email);
                return StatusCode(500, new { error = ex.Message, details = ex.ToString() });
            }
        }

        [HttpPost("me/notifications/{id:guid}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(
            Guid id,
            CancellationToken cancellationToken)
        {
            var notification = await dbContext.StudentNotifications.FindAsync([id], cancellationToken);
            if (notification == null)
            {
                return NotFound(new { error = "Notification not found." });
            }

            notification.IsRead = true;
            await dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Notification marked as read." });
        }
    }

    public class StudentHistoryResponseDto
    {
        public string StudentEmail { get; set; } = string.Empty;
        public int TotalTestsTaken { get; set; }
        public double AveragePercentage { get; set; }
        public double HighestPercentage { get; set; }
        public List<StudentExamAttemptDto> Attempts { get; set; } = [];
    }

    public class StudentExamAttemptDto
    {
        public Guid SessionId { get; set; }
        public int TargetYear { get; set; }
        public int TotalScore { get; set; }
        public int TotalQuestions { get; set; }
        public double Percentage { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
        public DateTimeOffset StartTime { get; set; }
    }
}
