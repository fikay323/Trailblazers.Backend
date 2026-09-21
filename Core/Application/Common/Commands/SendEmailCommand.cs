using MediatR;

namespace Trailblazers.Backend.Core.Application.Common.Commands
{
    public record SendEmailCommand(
        string To,
        string Subject,
        string Body,
        bool IsHtml = false
    ) : IRequest;
}
