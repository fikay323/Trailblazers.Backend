using MediatR;

namespace Trailblazers.Backend.Core.Application.Features.Exams.GetExamMetadata
{
    public record GetExamMetadataQuery : IRequest<ExamMetadataDto>;

    public record ExamMetadataDto(
        List<string> Subjects,
        List<int> Years
    );

    public class GetExamMetadataQueryHandler : IRequestHandler<GetExamMetadataQuery, ExamMetadataDto>
    {
        public Task<ExamMetadataDto> Handle(GetExamMetadataQuery request, CancellationToken cancellationToken)
        {
            var subjects = new List<string>
            {
                "Use of English",
                "Mathematics",
                "Physics",
                "Chemistry",
                "Biology",
                "Economics",
                "Government",
                "Literature in English",
                "Christian Religious Studies",
                "Geography",
                "Commerce"
            };

            var years = Enumerable.Range(1980, 2025 - 1980 + 1).OrderByDescending(y => y).ToList();

            return Task.FromResult(new ExamMetadataDto(subjects, years));
        }
    }
}
