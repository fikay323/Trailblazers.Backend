using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController(
        IExamSessionRepository sessionRepository,
        UserManager<ApplicationUser> userManager) : ControllerBase
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
                user.CreatedAt
            });
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
