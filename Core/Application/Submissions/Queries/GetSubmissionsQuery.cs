using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Repositories;

namespace Trailblazers.Backend.Core.Application.Submissions.Queries
{
    public record GetSubmissionsQuery(
        SubmissionType? Type = null,
        string? SearchTerm = null,
        DateTimeOffset? StartDate = null,
        DateTimeOffset? EndDate = null,
        int PageNumber = 1,
        int PageSize = 10
    );

    public record GetSubmissionsResponse(
        List<Submission> Items,
        int TotalCount
    );

    public class GetSubmissionsQueryHandler(ISubmissionRepository repository)
    {
        public async Task<GetSubmissionsResponse> HandleAsync(GetSubmissionsQuery query, CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var skip = (pageNumber - 1) * pageSize;

            var (items, totalCount) = await repository.GetSubmissionsOffsetAsync(
                query.Type,
                query.SearchTerm,
                query.StartDate,
                query.EndDate,
                skip,
                pageSize,
                cancellationToken
            );

            return new GetSubmissionsResponse(items, totalCount);
        }
    }
}
