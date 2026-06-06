using System.Net.Mail;
using System.Text.Json;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Repositories;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.Core.Application.Submissions.Commands
{
    public record SubmitRegistrationCommand(
        string Name,
        string Email,
        string PhoneNumber,
        string TargetExam
    );

    public class SubmitRegistrationCommandHandler(
        ISubmissionRepository repository,
        IMailService mailService,
        ILogger<SubmitRegistrationCommandHandler> logger)
    {
        public async Task<Submission> HandleAsync(SubmitRegistrationCommand command,
            CancellationToken cancellationToken = default)
        {
            Validate(command);

            var metadataJson = JsonSerializer.Serialize(new
            {
                PhoneNumber = command.PhoneNumber.Trim(),
                TargetExam = command.TargetExam.Trim()
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

            _ = Task.Run(async () =>
            {
                try
                {
                    var body = $"Dear {submission.Name},\n\n" +
                               $"Thank you for registering for the {command.TargetExam} preparation program with Trailblazers!\n" +
                               $"We have received your details (Phone: {command.PhoneNumber}) and will get back to you shortly.\n\n" +
                               $"Best regards,\n" +
                               $"The Trailblazers Team";

                    await mailService.SendEmailAsync(submission.Email,
                        $"Welcome to Trailblazers - {command.TargetExam} Registration", body);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending student welcome email for registration {SubmissionId}",
                        submission.Id);
                }
            }, CancellationToken.None);

            return submission;
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
