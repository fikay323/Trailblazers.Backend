using System.Threading.Channels;
using MediatR;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<IBaseRequest> _queue;

        public BackgroundTaskQueue()
        {
            // Bounded channel to prevent resource exhaustion under high load.
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _queue = Channel.CreateBounded<IBaseRequest>(options);
        }

        public async ValueTask QueueBackgroundWorkItemAsync(IBaseRequest workItem)
        {
            ArgumentNullException.ThrowIfNull(workItem);
            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<IBaseRequest> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
