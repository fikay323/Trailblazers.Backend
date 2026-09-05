using MediatR;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.Core.Application.Common.Commands
{
    public class SendEmailCommandHandler(
        IMailService mailService,
        ILogger<SendEmailCommandHandler> logger)
        : IRequestHandler<SendEmailCommand>
    {
        public async Task Handle(SendEmailCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                logger.LogInformation("Processing queued background email dispatch to {Recipient}", request.To);
                await mailService.SendEmailAsync(request.To, request.Subject, request.Body);
                logger.LogInformation("Successfully sent queued email to {Recipient}", request.To);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send background email to {Recipient} with subject '{Subject}'",
                    request.To, request.Subject);
                // Do not rethrow to prevent crashing the background worker loop
            }
        }
    }
}
