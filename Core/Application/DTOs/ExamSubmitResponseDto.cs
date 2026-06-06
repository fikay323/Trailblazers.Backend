namespace Trailblazers.Backend.Core.Application.DTOs
{
    public record ExamSubmitResponseDto(
        int Score,
        int TotalQuestions,
        DateTimeOffset CompletedAt
    );
}
