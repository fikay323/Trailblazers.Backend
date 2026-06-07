using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Repositories
{
    public class ExamSessionRepository(ApplicationDbContext context) : IExamSessionRepository
    {
        public async Task<ExamSession?> GetByIdAsync(Guid sessionId)
        {
            return await context.ExamSessions
                .Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task AddAsync(ExamSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            await context.ExamSessions.AddAsync(session);
            await context.SaveChangesAsync();
        }

        public Task UpdateAsync(ExamSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            context.ExamSessions.Update(session);
            return context.SaveChangesAsync();
        }

        public async Task AddResultAsync(ExamResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            await context.ExamResults.AddAsync(result);
        }

        public async Task<ExamResult?> GetResultBySessionIdAsync(Guid sessionId)
        {
            return await context.ExamResults
                .Include(r => r.Session)
                .ThenInclude(s => s!.Answers)
                .FirstOrDefaultAsync(r => r.SessionId == sessionId);
        }
    }
}
