using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Core.Application.Interfaces
{
    public interface IExamSessionRepository
    {
        Task<ExamSession?> GetByIdAsync(Guid sessionId);
        Task AddAsync(ExamSession session);
        Task UpdateAsync(ExamSession session);
        Task AddResultAsync(ExamResult result);
        Task<ExamResult?> GetResultBySessionIdAsync(Guid sessionId);
    }
}
