using MediatR;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IBackgroundTaskQueue
    {
        ValueTask QueueBackgroundWorkItemAsync(IBaseRequest workItem);
        ValueTask<IBaseRequest> DequeueAsync(CancellationToken cancellationToken);
    }
}
