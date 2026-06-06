using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Enums;

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
                if (Enum.TryParse<ExamSubject>(s, true, out var parsedSubject))
                {
                    subjectsEnumList.Add(parsedSubject);
                }
            }

            return await context.ExamQuestions
                .AsNoTracking()
                .Where(q => q.ExamYear == year && subjectsEnumList.Contains(q.Subject))
                .ToListAsync();
        }

        public async Task<IEnumerable<ExamQuestion>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            return await context.ExamQuestions
                .AsNoTracking()
                .Where(q => ids.Contains(q.Id))
                .ToListAsync();
        }
    }
}
