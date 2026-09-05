using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/students")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public class AdminStudentsController(
        UserManager<ApplicationUser> userManager,
        IExamSessionRepository sessionRepository,
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
    }

    public class AdminStudentListItemDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public string? DisabledReason { get; set; }
        public int TotalTestsTaken { get; set; }
        public double AverageScore { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class UpdateStudentStatusRequestDto
    {
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
    }
}
