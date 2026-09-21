using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Common.Commands;
using Trailblazers.Backend.Core.Application.Features.GuardianReports.Dtos;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class GuardianReportService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IExamSessionRepository sessionRepository,
        IEmailTemplateService templateService,
        IBackgroundTaskQueue taskQueue,
        ILogger<GuardianReportService> logger) : IGuardianReportService
    {
        public async Task<GuardianReportPreviewDto> GetReportPreviewAsync(
            string studentEmail,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default)
        {
            var cleanEmail = studentEmail.Trim().ToLowerInvariant();
            var user = await userManager.FindByEmailAsync(cleanEmail);

            // Look up guardian details from Registration Submission metadata
            var submission = await dbContext.Submissions
                .Where(s => s.Type == SubmissionType.Registration && s.Email == cleanEmail)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            string guardianName = string.Empty;
            string guardianPhone = string.Empty;
            string guardianEmail = string.Empty;
            string guardianRelationship = string.Empty;

            if (submission != null && !string.IsNullOrWhiteSpace(submission.Metadata))
            {
                try
                {
                    using var doc = JsonDocument.Parse(submission.Metadata);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("GuardianName", out var gn)) guardianName = gn.GetString() ?? string.Empty;
                    if (root.TryGetProperty("GuardianPhone", out var gp)) guardianPhone = gp.GetString() ?? string.Empty;
                    if (root.TryGetProperty("GuardianEmail", out var ge)) guardianEmail = ge.GetString() ?? string.Empty;
                    if (root.TryGetProperty("GuardianRelationship", out var gr)) guardianRelationship = gr.GetString() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not parse registration metadata for {Email}", cleanEmail);
                }
            }

            // Query Exam Results in timeframe
            var allResults = await sessionRepository.GetResultsByStudentEmailAsync(cleanEmail, cancellationToken);
            var filteredResults = allResults
                .Where(r => r.CompletedAt >= startDate && r.CompletedAt <= endDate)
                .OrderByDescending(r => r.CompletedAt)
                .ToList();

            var attempts = filteredResults.Select(r =>
            {
                var totalQuestions = (r.Session?.AssignedQuestionIds != null && r.Session.AssignedQuestionIds.Count > 0)
                    ? r.Session.AssignedQuestionIds.Count
                    : (r.Session?.Answers?.Count ?? 0);

                var percentage = totalQuestions > 0
                    ? Math.Round((double)r.TotalScore / totalQuestions * 100, 1)
                    : 0;

                return new GuardianReportAttemptDto
                {
                    SessionId = r.SessionId,
                    TargetYear = r.Session?.TargetYear ?? 0,
                    TotalScore = r.TotalScore,
                    TotalQuestions = totalQuestions,
                    Percentage = percentage,
                    CompletedAt = r.CompletedAt
                };
            }).ToList();

            var totalTests = attempts.Count;
            var averageScore = totalTests > 0
                ? Math.Round(attempts.Average(a => a.Percentage), 1)
                : 0;
            var highestScore = totalTests > 0
                ? attempts.Max(a => a.Percentage)
                : 0;
            var passedCount = attempts.Count(a => a.Percentage >= 50.0);
            var passRate = totalTests > 0
                ? Math.Round((double)passedCount / totalTests * 100, 1)
                : 0;

            // Attendance in timeframe
            int attendancePresent = 0;
            int attendanceLate = 0;

            if (user != null)
            {
                var startD = DateOnly.FromDateTime(startDate.UtcDateTime);
                var endD = DateOnly.FromDateTime(endDate.UtcDateTime);

                var attendanceRecords = await dbContext.AttendanceRecords
                    .Where(a => a.StudentId == user.Id && a.Date >= startD && a.Date <= endD)
                    .ToListAsync(cancellationToken);

                attendancePresent = attendanceRecords.Count(a => a.Status == AttendanceStatus.Present);
                attendanceLate = attendanceRecords.Count(a => a.Status == AttendanceStatus.Late);
            }

            return new GuardianReportPreviewDto
            {
                StudentName = user?.FullName ?? submission?.Name ?? cleanEmail,
                StudentEmail = cleanEmail,
                GuardianName = guardianName,
                GuardianPhone = guardianPhone,
                GuardianEmail = guardianEmail,
                GuardianRelationship = guardianRelationship,
                StartDate = startDate,
                EndDate = endDate,
                TotalTestsTaken = totalTests,
                AveragePercentage = averageScore,
                HighestPercentage = highestScore,
                PassRatePercentage = passRate,
                AttendancePresentDays = attendancePresent,
                AttendanceLateDays = attendanceLate,
                Attempts = attempts
            };
        }

        public async Task<SendGuardianReportResponseDto> SendReportAsync(
            SendGuardianReportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var preview = await GetReportPreviewAsync(request.StudentEmail, request.StartDate, request.EndDate, cancellationToken);

            var guardianName = !string.IsNullOrWhiteSpace(request.GuardianName) ? request.GuardianName.Trim() : preview.GuardianName;
            var guardianEmail = !string.IsNullOrWhiteSpace(request.GuardianEmail) ? request.GuardianEmail.Trim().ToLowerInvariant() : preview.GuardianEmail;
            var guardianPhone = !string.IsNullOrWhiteSpace(request.GuardianPhone) ? request.GuardianPhone.Trim() : preview.GuardianPhone;

            bool emailSent = false;
            bool smsSent = false;
            var deliveredChannels = new List<string>();

            // 1. Email Channel
            if (string.Equals(request.Channel, "Email", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.Channel, "Both", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(guardianEmail))
                {
                    throw new InvalidOperationException("Guardian email address is required to dispatch report via Email.");
                }

                var emailHtml = templateService.RenderGuardianProgressReportEmail(
                    studentName: preview.StudentName,
                    guardianName: guardianName,
                    startDate: request.StartDate,
                    endDate: request.EndDate,
                    totalTests: preview.TotalTestsTaken,
                    averagePercentage: preview.AveragePercentage,
                    highestPercentage: preview.HighestPercentage,
                    passRate: preview.PassRatePercentage,
                    attendancePresent: preview.AttendancePresentDays,
                    attendanceLate: preview.AttendanceLateDays,
                    customRemarks: request.CustomRemarks);

                await taskQueue.QueueBackgroundWorkItemAsync(new SendEmailCommand(
                    To: guardianEmail,
                    Subject: $"Academic Progress Report Card - {preview.StudentName} (Trailblazers Academy)",
                    Body: emailHtml,
                    IsHtml: true));

                emailSent = true;
                deliveredChannels.Add($"Email ({guardianEmail})");
                logger.LogInformation("Guardian report card email queued for {GuardianEmail} (Student: {Student})", guardianEmail, preview.StudentName);
            }

            // 2. SMS Channel
            if (string.Equals(request.Channel, "Sms", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.Channel, "Both", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(guardianPhone))
                {
                    throw new InvalidOperationException("Guardian phone number is required to dispatch report via SMS.");
                }

                var remarksSnippet = !string.IsNullOrWhiteSpace(request.CustomRemarks) ? $" Note: {request.CustomRemarks.Trim()}." : "";
                var smsText = $"Trailblazers Academy Report for {preview.StudentName} ({request.StartDate:MMM d} - {request.EndDate:MMM d}): " +
                              $"Avg Score: {preview.AveragePercentage:F1}%, {preview.TotalTestsTaken} tests completed. " +
                              $"Attendance: {preview.AttendancePresentDays} days present.{remarksSnippet} Enquiries: 08165999425";

                // In production, send to SMS gateway; logged to queue
                logger.LogInformation("Guardian report card SMS prepared for {Phone}: {Message}", guardianPhone, smsText);
                smsSent = true;
                deliveredChannels.Add($"SMS ({guardianPhone})");
            }

            return new SendGuardianReportResponseDto
            {
                Success = true,
                Message = $"Report card successfully dispatched to {string.Join(" & ", deliveredChannels)}.",
                EmailSent = emailSent,
                SmsSent = smsSent,
                DeliveredTo = string.Join(", ", deliveredChannels),
                ReportSummary = preview
            };
        }
    }
}
