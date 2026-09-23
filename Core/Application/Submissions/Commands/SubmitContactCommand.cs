using System.Net.Mail;
using System.Text.Json;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Repositories;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Application.Common.Commands;

namespace Trailblazers.Backend.Core.Application.Submissions.Commands
{
    public record SubmitContactCommand(
        string Name,
        string Email,
        string Message,
        string? Honeypot = null,
        long? FormLoadTimestamp = null
    );

    public class SubmitContactCommandHandler(
        ISubmissionRepository repository,
        IBackgroundTaskQueue taskQueue,
        ILogger<SubmitContactCommandHandler> logger)
    {
        public async Task<Submission> HandleAsync(SubmitContactCommand command,
            CancellationToken cancellationToken = default)
        {
            // Honeypot check
            if (!string.IsNullOrWhiteSpace(command.Honeypot))
            {
                logger.LogWarning("Bot contact inquiry dropped via honeypot: Name='{Name}', Email='{Email}'",
                    command.Name, command.Email);
                return CreateDummySubmission(command);
            }

            // Duration check (< 2 seconds)
            if (command.FormLoadTimestamp.HasValue)
            {
                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var elapsedSec = (nowMs - command.FormLoadTimestamp.Value) / 1000.0;
                if (elapsedSec < 2.0 || elapsedSec > 86400)
                {
                    logger.LogWarning("Bot contact inquiry dropped via timing check: Elapsed={ElapsedSec:F1}s, Email='{Email}'",
                        elapsedSec, command.Email);
                    return CreateDummySubmission(command);
                }
            }

            Validate(command);

            var metadataJson = JsonSerializer.Serialize(new { Message = command.Message.Trim() });

            var submission = new Submission
            {
                Type = SubmissionType.Contact,
                Name = command.Name.Trim(),
                Email = command.Email.Trim().ToLowerInvariant(),
                Metadata = metadataJson,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await repository.AddAsync(submission, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            var adminEmail = Environment.GetEnvironmentVariable("SMTP_ADMIN_EMAIL") ?? "admin@trailblazers.com";
            var body = $"New contact submission received:\n\n" +
                       $"Name: {submission.Name}\n" +
                       $"Email: {submission.Email}\n" +
                       $"Message: {command.Message}\n";

            await taskQueue.QueueBackgroundWorkItemAsync(new SendEmailCommand(adminEmail, "New Contact Inquiry", body));

            return submission;
        }

        private void Validate(SubmitContactCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Name))
                throw new ArgumentException("Name is required.", nameof(command.Name));

            if (string.IsNullOrWhiteSpace(command.Email))
                throw new ArgumentException("Email is required.", nameof(command.Email));

            if (!IsValidEmail(command.Email))
                throw new ArgumentException("Email format is invalid.", nameof(command.Email));

            if (string.IsNullOrWhiteSpace(command.Message))
                throw new ArgumentException("Message is required.", nameof(command.Message));
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

        private static Submission CreateDummySubmission(SubmitContactCommand command)
        {
            return new Submission
            {
                Id = Guid.NewGuid(),
                Type = SubmissionType.Contact,
                Name = command.Name ?? "Inquirer",
                Email = command.Email ?? "inquiry@trailblazer-academy.com",
                Metadata = "{}",
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
