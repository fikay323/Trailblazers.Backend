using System.Net.Mail;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Repositories;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Application.Common.Commands;

namespace Trailblazers.Backend.Core.Application.Submissions.Commands
{
    public record SubmitRegistrationCommand(
        string Name,
        string Email,
        string PhoneNumber,
        string TargetExam,
        string? GuardianName = null,
        string? GuardianPhone = null,
        string? GuardianEmail = null,
        string? GuardianRelationship = null,
        string? DateOfBirth = null,
        string? Gender = null,
        string? Address = null,
        string? LastSchool = null,
        string? ClassCompleted = null,
        string? SubjectCombination = null,
        string? ClassMode = null,
        string? Referral = null,
        List<string>? Programmes = null,
        string? Honeypot = null,
        long? FormLoadTimestamp = null
    );

    public class SubmitRegistrationCommandHandler(
        ISubmissionRepository repository,
        IBackgroundTaskQueue taskQueue,
        UserManager<ApplicationUser> userManager,
        IEmailTemplateService templateService,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<SubmitRegistrationCommandHandler> logger)
    {
        public async Task<Submission> HandleAsync(SubmitRegistrationCommand command,
            CancellationToken cancellationToken = default)
        {
            // --- Anti-Bot Defense Layer ---
            // 1. Honeypot check: If the hidden honeypot trap field was filled, silently swallow
            if (!string.IsNullOrWhiteSpace(command.Honeypot))
            {
                logger.LogWarning("Bot registration attempt dropped via honeypot trap: Name='{Name}', Email='{Email}', Honeypot='{Honeypot}'",
                    command.Name, command.Email, command.Honeypot);
                return CreateDummySubmission(command);
            }

            // 2. Submission duration check: Human filling this form takes at least 2.5 seconds
            if (command.FormLoadTimestamp.HasValue)
            {
                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var elapsedSec = (nowMs - command.FormLoadTimestamp.Value) / 1000.0;
                if (elapsedSec < 2.5 || elapsedSec > 86400)
                {
                    logger.LogWarning("Bot registration attempt dropped via timing check: Elapsed={ElapsedSec:F1}s, Email='{Email}'",
                        elapsedSec, command.Email);
                    return CreateDummySubmission(command);
                }
            }

            // 3. Bot Name Heuristic: Random consonant strings (e.g., 'Sqvhtgx')
            if (IsBotSpamName(command.Name))
            {
                logger.LogWarning("Bot registration attempt dropped via name heuristic: Name='{Name}', Email='{Email}'",
                    command.Name, command.Email);
                return CreateDummySubmission(command);
            }

            Validate(command);

            var metadataJson = JsonSerializer.Serialize(new
            {
                PhoneNumber = command.PhoneNumber.Trim(),
                TargetExam = command.TargetExam.Trim(),
                GuardianName = command.GuardianName?.Trim() ?? string.Empty,
                GuardianPhone = command.GuardianPhone?.Trim() ?? string.Empty,
                GuardianEmail = command.GuardianEmail?.Trim() ?? string.Empty,
                GuardianRelationship = command.GuardianRelationship?.Trim() ?? string.Empty,
                DateOfBirth = command.DateOfBirth?.Trim() ?? string.Empty,
                Gender = command.Gender?.Trim() ?? string.Empty,
                Address = command.Address?.Trim() ?? string.Empty,
                LastSchool = command.LastSchool?.Trim() ?? string.Empty,
                ClassCompleted = command.ClassCompleted?.Trim() ?? string.Empty,
                SubjectCombination = command.SubjectCombination?.Trim() ?? string.Empty,
                ClassMode = command.ClassMode?.Trim() ?? string.Empty,
                Referral = command.Referral?.Trim() ?? string.Empty,
                Programmes = command.Programmes ?? [],
                AccountCreated = false
            });

            var submission = new Submission
            {
                Type = SubmissionType.Registration,
                Name = command.Name.Trim(),
                Email = command.Email.Trim().ToLowerInvariant(),
                Metadata = metadataJson,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await repository.AddAsync(submission, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            // 1. Send confirmation email to the student
            var studentBody = $"Dear {submission.Name},\n\n" +
                       $"Thank you for submitting your registration for the {command.TargetExam} preparation program with Trailblazers Academy!\n" +
                       $"We have received your application. Our admissions team will review your information and follow up with you and your parent/guardian shortly.\n\n" +
                       $"Best regards,\n" +
                       $"Trailblazers Academy Admissions Team";

            await taskQueue.QueueBackgroundWorkItemAsync(new SendEmailCommand(
                submission.Email,
                $"Welcome to Trailblazers Academy - {command.TargetExam} Registration Received",
                studentBody));

            // 2. Notify all Administrators so they can follow up
            try
            {
                var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
                var reviewUrl = $"{ResolveAdminPortalUrl()}/admin/submissions?tab=students";

                var adminAlertHtml = templateService.RenderAdminNewRegistrationAlertEmail(
                    studentName: submission.Name,
                    studentEmail: submission.Email,
                    studentPhone: command.PhoneNumber,
                    targetExam: command.TargetExam,
                    guardianName: command.GuardianName,
                    guardianPhone: command.GuardianPhone,
                    guardianEmail: command.GuardianEmail,
                    guardianRelationship: command.GuardianRelationship,
                    reviewUrl: reviewUrl
                );

                foreach (var admin in adminUsers)
                {
                    if (!string.IsNullOrWhiteSpace(admin.Email))
                    {
                        await taskQueue.QueueBackgroundWorkItemAsync(new SendEmailCommand(
                            admin.Email,
                            $"[New Registration] {submission.Name} ({command.TargetExam})",
                            adminAlertHtml,
                            IsHtml: true));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue admin alert emails for new registration {SubmissionId}", submission.Id);
            }

            return submission;
        }

        private string ResolveAdminPortalUrl()
        {
            var envUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                      ?? Environment.GetEnvironmentVariable("APP_URL")
                      ?? configuration["FrontendUrl"];

            if (!string.IsNullOrWhiteSpace(envUrl))
            {
                return envUrl.TrimEnd('/');
            }

            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                if (httpContext.Request.Headers.TryGetValue("Origin", out var origin) && !string.IsNullOrWhiteSpace(origin))
                {
                    return origin.ToString().TrimEnd('/');
                }

                if (httpContext.Request.Headers.TryGetValue("Referer", out var referer) && !string.IsNullOrWhiteSpace(referer))
                {
                    if (Uri.TryCreate(referer.ToString(), UriKind.Absolute, out var refererUri))
                    {
                        return $"{refererUri.Scheme}://{refererUri.Authority}".TrimEnd('/');
                    }
                }
            }

            var isDev = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);
            return isDev ? "http://localhost:3000" : "https://staff.trailblazer-academy.com";
        }

        private void Validate(SubmitRegistrationCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
                throw new ArgumentException("Name is required.", nameof(command.Name));

            if (string.IsNullOrWhiteSpace(command.Email))
                throw new ArgumentException("Email is required.", nameof(command.Email));

            if (!IsValidEmail(command.Email))
                throw new ArgumentException("Email format is invalid.", nameof(command.Email));

            if (string.IsNullOrWhiteSpace(command.PhoneNumber))
                throw new ArgumentException("Phone number is required.", nameof(command.PhoneNumber));

            if (string.IsNullOrWhiteSpace(command.TargetExam))
                throw new ArgumentException("Target exam is required.", nameof(command.TargetExam));

            if (string.IsNullOrWhiteSpace(command.GuardianName))
                throw new ArgumentException("Parent/Guardian name is required.", nameof(command.GuardianName));

            if (string.IsNullOrWhiteSpace(command.GuardianPhone) && string.IsNullOrWhiteSpace(command.GuardianEmail))
                throw new ArgumentException("At least one parent/guardian contact method (phone number or email) is required.", nameof(command.GuardianPhone));

            if (!string.IsNullOrWhiteSpace(command.GuardianEmail) && !IsValidEmail(command.GuardianEmail))
                throw new ArgumentException("Guardian email format is invalid.", nameof(command.GuardianEmail));
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsBotSpamName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var trimmed = name.Trim();
            // Single-word consonant cluster of 5+ letters without vowels (e.g. Sqvhtgx)
            if (!trimmed.Contains(' ') && System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ]{5,}$"))
            {
                return true;
            }
            return false;
        }

        private static Submission CreateDummySubmission(SubmitRegistrationCommand command)
        {
            return new Submission
            {
                Id = Guid.NewGuid(),
                Type = SubmissionType.Registration,
                Name = command.Name ?? "Student",
                Email = command.Email ?? "student@trailblazer-academy.com",
                Metadata = "{}",
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
