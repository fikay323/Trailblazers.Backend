using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IExamQuestionRepository
    {
        Task<IEnumerable<ExamQuestion>> GetQuestionsByYearAndSubjectsAsync(int year, IEnumerable<string> subjects);
        Task<IEnumerable<ExamQuestion>> GetByIdsAsync(IEnumerable<Guid> ids);
    }
}
