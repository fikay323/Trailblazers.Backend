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

        public async Task<ExamResult> CompleteAndSaveResultAsync(ExamSession session, ExamResult result, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(result);

            session.Complete();

            try
            {
                await context.ExamResults.AddAsync(result, cancellationToken);
                context.ExamSessions.Update(session);
                await context.SaveChangesAsync(cancellationToken);
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency conflict: another concurrent request completed this session
                context.Entry(result).State = EntityState.Detached;
                context.Entry(session).State = EntityState.Detached;

                var existingResult = await GetResultBySessionIdAsync(session.Id);
                if (existingResult != null)
                {
                    return existingResult;
                }
                throw;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_exam_results_session_id") == true ||
                                              ex.InnerException?.Message.Contains("duplicate key") == true)
            {
                context.Entry(result).State = EntityState.Detached;
                context.Entry(session).State = EntityState.Detached;

                var existingResult = await GetResultBySessionIdAsync(session.Id);
                if (existingResult != null)
                {
                    return existingResult;
                }
                throw;
            }
        }
    }
}
