using System.Net.Mail;
using System.Text.Json;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Repositories;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.Core.Application.Submissions.Commands
{
    public record SubmitContactCommand(
        string Name,
        string Email,
        string Message
    );

    public class SubmitContactCommandHandler(
        ISubmissionRepository repository,
        IMailService mailService,
        ILogger<SubmitContactCommandHandler> logger)
    {
        public async Task<Submission> HandleAsync(SubmitContactCommand command,
            CancellationToken cancellationToken = default)
        {
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

            _ = Task.Run(async () =>
            {
                try
                {
                    var body = $"New contact submission received:\n\n" +
                               $"Name: {submission.Name}\n" +
                               $"Email: {submission.Email}\n" +
                               $"Message: {command.Message}\n";

                    await mailService.SendEmailAsync(adminEmail, "New Contact Inquiry", body);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending admin notification email for contact submission {SubmissionId}",
                        submission.Id);
                }
            }, CancellationToken.None);

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
    }
}
