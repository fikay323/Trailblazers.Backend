using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Enums;
using Trailblazers.Backend.Infrastructure.Extensions;

namespace Trailblazers.Backend.Infrastructure.Persistence.Repositories
{
    public class ExamQuestionRepository(ApplicationDbContext context) : IExamQuestionRepository
    {
        public async Task<IEnumerable<ExamQuestion>> GetQuestionsByYearAndSubjectsAsync(int year,
            IEnumerable<string> subjects)
        {
            var subjectsEnumList = new List<ExamSubject>();
            foreach (var s in subjects)
            {
                var parsedSubject = ExamSubjectExtensions.ToExamSubject(s);
                if (parsedSubject.HasValue)
                {
                    subjectsEnumList.Add(parsedSubject.Value);
                }
            }

            var finalQuestions = new List<ExamQuestion>();
            foreach (var subject in subjectsEnumList)
            {
                var threshold = subject == ExamSubject.English ? 50 : 40;

                // Sample primary questions directly in PostgreSQL using RANDOM()
                var primaryQuestions = await context.ExamQuestions
                    .AsNoTracking()
                    .Where(q => q.Subject == subject && q.ExamYear == year)
                    .OrderBy(_ => EF.Functions.Random())
                    .Take(threshold)
                    .ToListAsync();

                if (primaryQuestions.Count < threshold)
                {
                    var deficit = threshold - primaryQuestions.Count;
                    var primaryIds = primaryQuestions.Select(q => q.Id).ToList();

                    // Sample backfill questions directly in PostgreSQL using RANDOM() without loading the entire DB table
                    var backfillQuestions = await context.ExamQuestions
                        .AsNoTracking()
                        .Where(q => q.Subject == subject && q.ExamYear != year && !primaryIds.Contains(q.Id))
                        .OrderBy(_ => EF.Functions.Random())
                        .Take(deficit)
                        .ToListAsync();

                    primaryQuestions.AddRange(backfillQuestions);
                }

                finalQuestions.AddRange(primaryQuestions);
            }

            return finalQuestions;
        }

        public async Task<IEnumerable<ExamQuestion>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            return await context.ExamQuestions
                .AsNoTracking()
                .Where(q => ids.Contains(q.Id))
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(ExamSubject subject, int year, IEnumerable<string> examTypes, CancellationToken cancellationToken = default)
        {
            var typesList = examTypes.ToList();
            return await context.ExamQuestions
                .AnyAsync(q => q.Subject == subject && q.ExamYear == year && typesList.Contains(q.ExamType),
                    cancellationToken);
        }

        public async Task<int> AddRangeAsync(IEnumerable<ExamQuestion> questions, CancellationToken cancellationToken = default)
        {
            var list = questions.ToList();
            if (list.Count == 0) return 0;

            await context.ExamQuestions.AddRangeAsync(list, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return list.Count;
        }

        public async Task<HashSet<(int AlocId, string ExamType)>> GetExistingAlocPairsAsync(IEnumerable<int> alocIds, IEnumerable<string> examTypes, CancellationToken cancellationToken = default)
        {
            var alocList = alocIds.ToList();
            var typesList = examTypes.Distinct().ToList();

            var pairs = await context.ExamQuestions
                .Where(q => alocList.Contains(q.AlocId) && typesList.Contains(q.ExamType))
                .Select(q => new { q.AlocId, q.ExamType })
                .ToListAsync(cancellationToken);

            return pairs.Select(p => (p.AlocId, p.ExamType)).ToHashSet();
        }
    }
}
