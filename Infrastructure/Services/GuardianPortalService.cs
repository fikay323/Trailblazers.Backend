using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Common.Commands;
using Trailblazers.Backend.Core.Application.Features.GuardianPortal.Dtos;
using Trailblazers.Backend.Core.Application.Features.GuardianPortal.Interfaces;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class GuardianPortalService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IExamSessionRepository sessionRepository,
        IEmailTemplateService templateService,
        IBackgroundTaskQueue taskQueue,
        IConfiguration configuration,
        ILogger<GuardianPortalService> logger) : IGuardianPortalService
    {
        private string GetSecretKey() =>
            configuration["Jwt:Key"] ?? "TrailblazersAcademySuperSecretGuardianPortalKey2026!#$";

        public async Task<GuardianAccessResponseDto> VerifyAccessAsync(
            GuardianAccessRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.StudentEmail) || string.IsNullOrWhiteSpace(request.GuardianContact))
            {
                return new GuardianAccessResponseDto
                {
                    Success = false,
                    Message = "Student email and Guardian contact (phone or email) are required."
                };
            }

            var cleanEmail = request.StudentEmail.Trim().ToLowerInvariant();
            var cleanContact = request.GuardianContact.Trim();

            var user = await userManager.FindByEmailAsync(cleanEmail);
            var submission = await dbContext.Submissions
                .Where(s => s.Type == SubmissionType.Registration && s.Email == cleanEmail)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null && submission == null)
            {
                return new GuardianAccessResponseDto
                {
                    Success = false,
                    Message = "No student record found matching this email address."
                };
            }

            string guardianName = string.Empty;
            string guardianPhone = string.Empty;
            string guardianEmail = string.Empty;
            string guardianRelationship = string.Empty;
            string studentPhone = user?.PhoneNumber ?? string.Empty;
            string targetExam = "JAMB/UTME";

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
                    if (root.TryGetProperty("PhoneNumber", out var sp) && string.IsNullOrWhiteSpace(studentPhone)) studentPhone = sp.GetString() ?? string.Empty;
                    if (root.TryGetProperty("TargetExam", out var te)) targetExam = te.GetString() ?? targetExam;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error parsing submission metadata for {Email}", cleanEmail);
                }
            }

            // Verify contact matches guardian email or guardian phone (or student phone as emergency fallback)
            bool isMatch = MatchesContact(cleanContact, guardianEmail, guardianPhone, studentPhone);

            if (!isMatch)
            {
                return new GuardianAccessResponseDto
                {
                    Success = false,
                    Message = "The guardian contact provided does not match our records for this student. Please check your phone number or email and try again."
                };
            }

            var token = GenerateAccessToken(cleanEmail);
            var overview = await BuildWardOverviewAsync(cleanEmail, user, submission, guardianName, guardianPhone, guardianEmail, guardianRelationship, targetExam, studentPhone, cancellationToken);

            return new GuardianAccessResponseDto
            {
                Success = true,
                Message = "Guardian access verified successfully.",
                AccessToken = token,
                WardOverview = overview
            };
        }

        public async Task<GuardianWardOverviewDto> GetWardOverviewAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            if (!TryValidateAccessToken(accessToken, out var studentEmail))
            {
                throw new UnauthorizedAccessException("The guardian access session has expired or is invalid. Please sign in again.");
            }

            var user = await userManager.FindByEmailAsync(studentEmail);
            var submission = await dbContext.Submissions
                .Where(s => s.Type == SubmissionType.Registration && s.Email == studentEmail)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            string guardianName = string.Empty;
            string guardianPhone = string.Empty;
            string guardianEmail = string.Empty;
            string guardianRelationship = string.Empty;
            string studentPhone = user?.PhoneNumber ?? string.Empty;
            string targetExam = "JAMB/UTME";

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
                    if (root.TryGetProperty("PhoneNumber", out var sp) && string.IsNullOrWhiteSpace(studentPhone)) studentPhone = sp.GetString() ?? string.Empty;
                    if (root.TryGetProperty("TargetExam", out var te)) targetExam = te.GetString() ?? targetExam;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error parsing submission metadata for {Email}", studentEmail);
                }
            }

            return await BuildWardOverviewAsync(studentEmail, user, submission, guardianName, guardianPhone, guardianEmail, guardianRelationship, targetExam, studentPhone, cancellationToken);
        }

        public async Task<GuardianInquiryResponseDto> SubmitInquiryAsync(
            GuardianInquiryRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.StudentEmail) || string.IsNullOrWhiteSpace(request.Message))
            {
                return new GuardianInquiryResponseDto
                {
                    Success = false,
                    Message = "Student email and inquiry message are required."
                };
            }

            var cleanEmail = request.StudentEmail.Trim().ToLowerInvariant();
            var user = await userManager.FindByEmailAsync(cleanEmail);
            var studentName = user?.FullName ?? cleanEmail;

            var guardianName = !string.IsNullOrWhiteSpace(request.GuardianName) ? request.GuardianName.Trim() : "Parent / Guardian";
            var guardianContact = !string.IsNullOrWhiteSpace(request.GuardianEmail)
                ? request.GuardianEmail.Trim()
                : (!string.IsNullOrWhiteSpace(request.GuardianPhone) ? request.GuardianPhone.Trim() : "Not provided");
            var subject = !string.IsNullOrWhiteSpace(request.Subject) ? request.Subject.Trim() : "General Inquiry";

            // 1. Alert Academy Admins
            try
            {
                var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
                var alertHtml = templateService.RenderGuardianInquiryAdminAlertEmail(
                    studentName: studentName,
                    studentEmail: cleanEmail,
                    guardianName: guardianName,
                    guardianContact: guardianContact,
                    subject: subject,
                    message: request.Message.Trim()
                );

                foreach (var admin in adminUsers)
                {
                    if (!string.IsNullOrWhiteSpace(admin.Email))
                    {
                        await taskQueue.QueueBackgroundWorkItemAsync(new SendEmailCommand(
                            admin.Email,
                            $"[Guardian Inquiry] {subject} - Ward: {studentName}",
                            alertHtml));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue admin alert email for guardian inquiry from {Contact}", guardianContact);
            }

            // 2. Send confirmation to guardian if email provided
            if (!string.IsNullOrWhiteSpace(request.GuardianEmail))
            {
                try
                {
                    var confirmHtml = templateService.RenderGuardianInquiryConfirmationEmail(
                        guardianName: guardianName,
                        studentName: studentName,
                        subject: subject,
                        message: request.Message.Trim()
                    );

                    await taskQueue.QueueBackgroundWorkItemAsync(new SendEmailCommand(
                        request.GuardianEmail.Trim().ToLowerInvariant(),
                        $"Inquiry Received: {subject} - Trailblazers Academy",
                        confirmHtml));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to queue confirmation email to guardian {GuardianEmail}", request.GuardianEmail);
                }
            }

            return new GuardianInquiryResponseDto
            {
                Success = true,
                Message = "Your inquiry has been sent to the academy administration. We will review it and get back to you shortly."
            };
        }

        public string GenerateAccessToken(string studentEmail)
        {
            var expiry = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds();
            var payload = $"{studentEmail.Trim().ToLowerInvariant()}:{expiry}";
            var keyBytes = Encoding.UTF8.GetBytes(GetSecretKey());
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var signature = Convert.ToHexString(hashBytes);
            var tokenRaw = $"{payload}:{signature}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenRaw));
        }

        public bool TryValidateAccessToken(string accessToken, out string studentEmail)
        {
            studentEmail = string.Empty;
            if (string.IsNullOrWhiteSpace(accessToken)) return false;

            try
            {
                var tokenRaw = Encoding.UTF8.GetString(Convert.FromBase64String(accessToken));
                var parts = tokenRaw.Split(':');
                if (parts.Length != 3) return false;

                var email = parts[0];
                if (!long.TryParse(parts[1], out var expirySec)) return false;
                var providedSig = parts[2];

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expirySec)
                {
                    return false; // Expired
                }

                var payload = $"{email}:{expirySec}";
                var keyBytes = Encoding.UTF8.GetBytes(GetSecretKey());
                using var hmac = new HMACSHA256(keyBytes);
                var expectedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

                if (!string.Equals(providedSig, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    return false; // Signature mismatch
                }

                studentEmail = email;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<GuardianWardOverviewDto> BuildWardOverviewAsync(
            string studentEmail,
            ApplicationUser? user,
            Submission? submission,
            string guardianName,
            string guardianPhone,
            string guardianEmail,
            string guardianRelationship,
            string targetExam,
            string studentPhone,
            CancellationToken cancellationToken)
        {
            // Exam Results
            var allResults = (await sessionRepository.GetResultsByStudentEmailAsync(studentEmail, cancellationToken)).ToList();
            var attempts = allResults
                .OrderByDescending(r => r.CompletedAt)
                .Select(r =>
                {
                    var totalQuestions = (r.Session?.AssignedQuestionIds != null && r.Session.AssignedQuestionIds.Count > 0)
                        ? r.Session.AssignedQuestionIds.Count
                        : (r.Session?.Answers?.Count ?? 0);

                    var percentage = totalQuestions > 0
                        ? Math.Round((double)r.TotalScore / totalQuestions * 100, 1)
                        : 0;

                    return new GuardianWardExamDto
                    {
                        SessionId = r.SessionId,
                        TargetYear = r.Session?.TargetYear ?? 0,
                        TotalScore = r.TotalScore,
                        TotalQuestions = totalQuestions,
                        Percentage = percentage,
                        Passed = percentage >= 50.0,
                        CompletedAt = r.CompletedAt
                    };
                }).ToList();

            var totalExams = attempts.Count;
            var avgPercentage = totalExams > 0 ? Math.Round(attempts.Average(a => a.Percentage), 1) : 0;
            var maxPercentage = totalExams > 0 ? attempts.Max(a => a.Percentage) : 0;
            var passedCount = attempts.Count(a => a.Passed);
            var passRate = totalExams > 0 ? Math.Round((double)passedCount / totalExams * 100, 1) : 0;

            // Attendance
            int presentDays = 0;
            int lateDays = 0;
            int totalDays = 0;
            var recentAttendance = new List<GuardianWardAttendanceDto>();

            if (user != null)
            {
                var attendanceRecords = await dbContext.AttendanceRecords
                    .Where(a => a.StudentId == user.Id)
                    .OrderByDescending(a => a.Date)
                    .Take(30)
                    .ToListAsync(cancellationToken);

                totalDays = attendanceRecords.Count;
                presentDays = attendanceRecords.Count(a => a.Status == AttendanceStatus.Present);
                lateDays = attendanceRecords.Count(a => a.Status == AttendanceStatus.Late);

                recentAttendance = attendanceRecords.Select(a => new GuardianWardAttendanceDto
                {
                    Id = a.Id,
                    Date = a.Date,
                    ClockInTime = a.ClockInTime,
                    ClockOutTime = a.ClockOutTime,
                    Status = a.Status.ToString(),
                    Remarks = a.Remarks
                }).ToList();
            }

            var attendanceRate = totalDays > 0 ? Math.Round((double)(presentDays + lateDays) / totalDays * 100, 1) : 100;

            // Announcements
            var now = DateTimeOffset.UtcNow;
            var notices = await dbContext.Announcements
                .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now))
                .Where(a => a.TargetAudience == "All" || a.TargetAudience == "Guardians" || a.TargetAudience == targetExam)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new GuardianWardAnnouncementDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Content = a.Content,
                    Priority = a.Priority.ToString(),
                    TargetAudience = a.TargetAudience,
                    AuthorName = a.AuthorName,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new GuardianWardOverviewDto
            {
                StudentName = user?.FullName ?? submission?.Name ?? studentEmail,
                StudentEmail = studentEmail,
                StudentPhone = studentPhone,
                TargetExam = targetExam,
                Status = user != null ? (user.IsActive ? "Enrolled & Active" : "Suspended") : "Registration Pending",
                EnrolledAt = user?.CreatedAt ?? submission?.CreatedAt,
                GuardianName = !string.IsNullOrWhiteSpace(guardianName) ? guardianName : "Parent / Guardian",
                GuardianRelationship = !string.IsNullOrWhiteSpace(guardianRelationship) ? guardianRelationship : "Guardian",
                GuardianPhone = guardianPhone,
                GuardianEmail = guardianEmail,
                TotalExamsTaken = totalExams,
                AverageScorePercentage = avgPercentage,
                HighestScorePercentage = maxPercentage,
                PassRatePercentage = passRate,
                RecentExams = attempts,
                TotalAttendanceRecorded = totalDays,
                PresentDays = presentDays,
                LateDays = lateDays,
                AttendanceRatePercentage = attendanceRate,
                RecentAttendance = recentAttendance,
                Announcements = notices
            };
        }

        private static bool MatchesContact(string input, string guardianEmail, string guardianPhone, string studentPhone)
        {
            var cleanInput = input.Trim().ToLowerInvariant();

            // Email comparison
            if (cleanInput.Contains('@'))
            {
                if (!string.IsNullOrWhiteSpace(guardianEmail) && string.Equals(cleanInput, guardianEmail.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Phone comparison: normalize digits
            var inputDigits = new string(cleanInput.Where(char.IsDigit).ToArray());
            if (inputDigits.Length >= 7)
            {
                var cleanGuardianPhoneDigits = new string(guardianPhone.Where(char.IsDigit).ToArray());
                var cleanStudentPhoneDigits = new string(studentPhone.Where(char.IsDigit).ToArray());

                if (cleanGuardianPhoneDigits.Length >= 7 &&
                    (inputDigits.EndsWith(cleanGuardianPhoneDigits) || cleanGuardianPhoneDigits.EndsWith(inputDigits) || inputDigits == cleanGuardianPhoneDigits))
                {
                    return true;
                }

                if (cleanStudentPhoneDigits.Length >= 7 &&
                    (inputDigits.EndsWith(cleanStudentPhoneDigits) || cleanStudentPhoneDigits.EndsWith(inputDigits) || inputDigits == cleanStudentPhoneDigits))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
