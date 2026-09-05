using System.Net.Mail;
using System.Text.Json;
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
        string? DateOfBirth = null,
        string? Gender = null,
        string? Address = null,
        string? LastSchool = null,
        string? ClassCompleted = null,
        string? SubjectCombination = null,
        string? ClassMode = null,
        string? Referral = null,
        List<string>? Programmes = null
    );

    public class SubmitRegistrationCommandHandler(
        ISubmissionRepository repository,
        IBackgroundTaskQueue taskQueue,
        ILogger<SubmitRegistrationCommandHandler> logger)
    {
        public async Task<Submission> HandleAsync(SubmitRegistrationCommand command,
            CancellationToken cancellationToken = default)
        {
            Validate(command);

            var metadataJson = JsonSerializer.Serialize(new
            {
                PhoneNumber = command.PhoneNumber.Trim(),
                TargetExam = command.TargetExam.Trim(),
                DateOfBirth = command.DateOfBirth?.Trim() ?? string.Empty,
                Gender = command.Gender?.Trim() ?? string.Empty,
                Address = command.Address?.Trim() ?? string.Empty,
                LastSchool = command.LastSchool?.Trim() ?? string.Empty,
                ClassCompleted = command.ClassCompleted?.Trim() ?? string.Empty,
                SubjectCombination = command.SubjectCombination?.Trim() ?? string.Empty,
                ClassMode = command.ClassMode?.Trim() ?? string.Empty,
                Referral = command.Referral?.Trim() ?? string.Empty,
                Programmes = command.Programmes ?? []
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

            var body = $"Dear {submission.Name},\n\n" +
                       $"Thank you for registering for the {command.TargetExam} preparation program with Trailblazers!\n" +
                       $"We have received your details (Phone: {command.PhoneNumber}) and will get back to you shortly.\n\n" +
                       $"Best regards,\n" +
                       $"The Trailblazers Team";

            await taskQueue.QueueBackgroundWorkItemAsync(new SendEmailCommand(
                submission.Email,
                $"Welcome to Trailblazers - {command.TargetExam} Registration",
                body));

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
