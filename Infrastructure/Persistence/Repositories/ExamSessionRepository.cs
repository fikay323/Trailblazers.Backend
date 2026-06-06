using System.Collections.Concurrent;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Repositories
{
    public class ExamSessionRepository : IExamSessionRepository
    {
        private static readonly ConcurrentDictionary<Guid, ExamSession> Sessions = new();

        public Task<ExamSession?> GetByIdAsync(Guid sessionId)
        {
            Sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public Task AddAsync(ExamSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            Sessions[session.Id] = session;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ExamSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            Sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}
