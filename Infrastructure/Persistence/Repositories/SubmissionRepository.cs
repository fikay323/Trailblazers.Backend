using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Repositories;

namespace Trailblazers.Backend.Infrastructure.Persistence.Repositories
{
    public class SubmissionRepository(ApplicationDbContext context) : ISubmissionRepository
    {
        public async Task AddAsync(Submission submission, CancellationToken cancellationToken = default)
        {
            await context.Submissions.AddAsync(submission, cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<(List<Submission> Items, int TotalCount)> GetSubmissionsOffsetAsync(
            SubmissionType? type,
            string? searchTerm,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = context.Submissions.AsNoTracking();

            if (type.HasValue)
            {
                query = query.Where(s => s.Type == type.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(s => s.Name.ToLower().Contains(term) || s.Email.ToLower().Contains(term));
            }

            if (startDate.HasValue)
            {
                query = query.Where(s => s.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.CreatedAt <= endDate.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
