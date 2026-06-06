using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Core.Domain.Repositories
{
    public interface ISubmissionRepository
    {
        Task AddAsync(Submission submission, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<(List<Submission> Items, int TotalCount)> GetSubmissionsOffsetAsync(
            SubmissionType? type,
            string? searchTerm,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
    }
}
