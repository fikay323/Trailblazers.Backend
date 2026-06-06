using MediatR;
using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Enums;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Core.Application.Features.Exams.SeedQuestions
{
    public record SeedAllQuestionsCommand : IRequest;

    public class SeedAllQuestionsCommandHandler(
        IServiceProvider serviceProvider,
        ILogger<SeedAllQuestionsCommandHandler> logger)
        : IRequestHandler<SeedAllQuestionsCommand>
    {
        public async Task Handle(SeedAllQuestionsCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting bulk JAMB mock exam questions database seeding job...");

            int currentYear = DateTime.UtcNow.Year;
            var subjects = Enum.GetValues<ExamSubject>();

            foreach (var subject in subjects)
            {
                logger.LogInformation("Seeding questions for subject enum: {Subject}", subject);

                for (int year = currentYear; year >= 2011; year--)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogWarning("Seeding operation was cancelled.");
                        return;
                    }

                    try
                    {
                        // Create a separate scope for DbContext to avoid tracking/concurrency issues during the long loop
                        using var scope = serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var apiService = scope.ServiceProvider.GetRequiredService<IJambApiService>();

                        // Check if questions already exist for this combination
                        var exists = await dbContext.ExamQuestions
                            .AnyAsync(q => q.Subject == subject && q.ExamYear == year, cancellationToken);

                        if (exists)
                        {
                            logger.LogInformation("Questions for {Subject} ({Year}) already exist. Skipping.", subject,
                                year);
                            continue;
                        }

                        logger.LogInformation("Fetching questions for {Subject} ({Year}) from RapidAPI...", subject,
                            year);
                        var fetched = (await apiService.FetchQuestionsAsync(subject, year, 40)).ToList();

                        if (fetched.Count > 0)
                        {
                            logger.LogInformation(
                                "Successfully fetched {Count} questions. Batch inserting to database...",
                                fetched.Count);
                            await dbContext.ExamQuestions.AddRangeAsync(fetched, cancellationToken);
                            await dbContext.SaveChangesAsync(cancellationToken);
                            logger.LogInformation("Seeded questions for {Subject} ({Year}) successfully.", subject,
                                year);
                        }
                        else
                        {
                            logger.LogWarning("No questions returned from API for {Subject} ({Year}).", subject, year);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred while seeding questions for {Subject} ({Year}).",
                            subject, year);
                    }

                    // Respect rate limits and sleep 2000ms between inner loop iterations
                    await Task.Delay(2000, cancellationToken);
                }
            }

            logger.LogInformation("Seeding job finished processing all subjects and years.");
        }
    }
}
