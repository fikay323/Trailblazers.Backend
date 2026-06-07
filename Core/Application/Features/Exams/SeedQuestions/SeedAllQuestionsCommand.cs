using MediatR;
using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Enums;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Core.Application.Features.Exams.SeedQuestions
{
    public record SeedAllQuestionsCommand : IRequest;

    public class SeedAllQuestionsCommandHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<SeedAllQuestionsCommandHandler> logger)
        : IRequestHandler<SeedAllQuestionsCommand>
    {
        public async Task Handle(SeedAllQuestionsCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting bulk JAMB mock exam questions database seeding job...");

            int currentYear = DateTime.UtcNow.Year;
            var subjects = Enum.GetValues<ExamSubject>();
            var queue = new List<(ExamSubject Subject, int Year)>();

            foreach (var subject in subjects)
            {
                for (int year = currentYear - 2; year >= 2000; year--)
                {
                    queue.Add((subject, year));
                }
            }

            logger.LogInformation("Flattened queue contains {Count} subject-year combinations.", queue.Count);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(queue, parallelOptions, async (item, ct) =>
            {
                var (subject, year) = item;

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var apiService = scope.ServiceProvider.GetRequiredService<IJambApiService>();

                    var fetched = (await apiService.FetchQuestionsAsync(subject, year, 40)).ToList();

                    if (fetched.Count > 0)
                    {
                        var fetchedAlocIds = fetched.Select(q => q.AlocId).ToList();

                        var existingIds = await dbContext.ExamQuestions
                            .Where(q => fetchedAlocIds.Contains(q.AlocId))
                            .Select(q => q.AlocId)
                            .ToListAsync(ct);

                        var newQuestionsToInsert = fetched.Where(q => !existingIds.Contains(q.AlocId)).ToList();

                        if (newQuestionsToInsert.Count > 0)
                        {
                            logger.LogInformation(
                                "Found {NewCount} new questions out of {TotalFetched} fetched for {Subject} ({Year}). Batch inserting...",
                                newQuestionsToInsert.Count, fetched.Count, subject, year);

                            await dbContext.ExamQuestions.AddRangeAsync(newQuestionsToInsert, ct);
                            await dbContext.SaveChangesAsync(ct);

                            logger.LogInformation("Seeded questions for {Subject} ({Year}) successfully.", subject,
                                year);
                        }
                        else
                        {
                            logger.LogInformation(
                                "All {TotalFetched} fetched questions for {Subject} ({Year}) already exist. Skipping database insert.",
                                fetched.Count, subject, year);
                        }
                    }
                    else
                    {
                        logger.LogWarning(
                            "No questions returned from API for {Subject} ({Year}) (or iteration was skipped).",
                            subject, year);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding questions for {Subject} ({Year}).", subject,
                        year);
                }
            });

            logger.LogInformation("Seeding job finished processing all subjects and years.");
        }
    }
}
