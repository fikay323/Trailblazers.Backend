using MediatR;
using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Enums;
using Trailblazers.Backend.Infrastructure.Persistence;

namespace Trailblazers.Backend.Core.Application.Features.Exams.SeedQuestions
{
    public record SeedQuestionsCommand(
        string Subject,
        int Year,
        int Amount
    ) : IRequest<int>;

    public class SeedQuestionsCommandHandler(
        IJambApiService apiService,
        ApplicationDbContext dbContext,
        ILogger<SeedQuestionsCommandHandler> logger)
        : IRequestHandler<SeedQuestionsCommand, int>
    {
        public async Task<int> Handle(SeedQuestionsCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            logger.LogInformation(
                "Processing SeedQuestionsCommand for Subject: {Subject}, Year: {Year}, Amount: {Amount}",
                request.Subject, request.Year, request.Amount);

            // Parse subject string to ExamSubject enum
            if (!Enum.TryParse<ExamSubject>(request.Subject, true, out var subjectEnum))
            {
                logger.LogError("Invalid subject string provided for seeding: {Subject}", request.Subject);
                throw new ArgumentException($"Subject {request.Subject} is not a valid, supported JAMB subject.");
            }

            // 1. Fetch transformed questions from the external RapidAPI
            var fetchedQuestions = (await apiService.FetchQuestionsAsync(subjectEnum, request.Year, request.Amount))
                .ToList();
            if (fetchedQuestions.Count == 0)
            {
                logger.LogWarning("No questions fetched from external API for Subject: {Subject}, Year: {Year}",
                    request.Subject, request.Year);
                return 0;
            }

            // 2. Check if questions for this subject/year combination already exist to avoid duplicates
            var exists = await dbContext.ExamQuestions
                .AnyAsync(q => q.Subject == subjectEnum && q.ExamYear == request.Year,
                    cancellationToken);

            if (exists)
            {
                logger.LogWarning(
                    "Questions already exist in database for Subject: {Subject}, Year: {Year}. Skipping Ingestion.",
                    request.Subject, request.Year);
                return 0;
            }

            // 3. Batch insert and save changes
            logger.LogInformation(
                "Batch inserting {Count} questions into database for Subject: {Subject}, Year: {Year}...",
                fetchedQuestions.Count, request.Subject, request.Year);
            await dbContext.ExamQuestions.AddRangeAsync(fetchedQuestions, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully persisted {Count} seeded questions into PostgreSQL.",
                fetchedQuestions.Count);

            return fetchedQuestions.Count;
        }
    }
}
