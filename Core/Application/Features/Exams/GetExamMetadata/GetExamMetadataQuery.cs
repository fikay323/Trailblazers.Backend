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
                "English", "Mathematics", "Biology", "Chemistry", "Physics",
                "Geography", "Civic Education", "Government", "Literature in English",
                "Economics", "Commerce", "Christian Religious Studies",
                "Islamic Religious Studies", "History"
            };

            var years = Enumerable.Range(1980, 2025 - 1980 + 1).OrderByDescending(y => y).ToList();

            return Task.FromResult(new ExamMetadataDto(subjects, years));
        }
    }
}
