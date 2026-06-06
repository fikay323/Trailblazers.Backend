
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Enums;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IJambApiService
    {
        Task<IEnumerable<ExamQuestion>> FetchQuestionsAsync(ExamSubject subject, int year, int limit);
    }
}
