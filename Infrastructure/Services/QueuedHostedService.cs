using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trailblazers.Backend.Core.Application.Interfaces;

namespace Trailblazers.Backend.Infrastructure.Services
{
    public class QueuedHostedService(
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<QueuedHostedService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Queued Hosted Service is starting and listening to the queue.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = await taskQueue.DequeueAsync(stoppingToken);

                    logger.LogInformation("Dequeued command {CommandName}. Executing...", workItem.GetType().Name);

                    // Execute inside a new scope to properly manage scoped dependencies (like DbContext)
                    using (var scope = serviceScopeFactory.CreateScope())
                    {
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        await mediator.Send(workItem, stoppingToken);
                    }

                    logger.LogInformation("Successfully executed command {CommandName}.", workItem.GetType().Name);
                }
                catch (OperationCanceledException)
                {
                    // Clean exit during application shutdown
                    logger.LogWarning("Queued Hosted Service was cancelled due to application shutdown.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while executing a background task.");
                }
            }

            logger.LogInformation("Queued Hosted Service has completed shutting down.");
        }
    }
}
