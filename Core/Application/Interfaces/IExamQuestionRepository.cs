using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Enums;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IExamQuestionRepository
    {
        Task<IEnumerable<ExamQuestion>> GetQuestionsByYearAndSubjectsAsync(int year, IEnumerable<string> subjects);
        Task<IEnumerable<ExamQuestion>> GetByIdsAsync(IEnumerable<Guid> ids);
        Task<bool> ExistsAsync(ExamSubject subject, int year, IEnumerable<string> examTypes, CancellationToken cancellationToken = default);
        Task<int> AddRangeAsync(IEnumerable<ExamQuestion> questions, CancellationToken cancellationToken = default);
        Task<HashSet<(int AlocId, string ExamType)>> GetExistingAlocPairsAsync(IEnumerable<int> alocIds, IEnumerable<string> examTypes, CancellationToken cancellationToken = default);
    }
}
