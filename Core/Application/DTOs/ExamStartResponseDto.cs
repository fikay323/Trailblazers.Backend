namespace Trailblazers.Backend.Core.Application.DTOs
{
    public record ExamStartResponseDto(
        Guid SessionId,
        DateTimeOffset EndTime,
        List<QuestionDto> Questions
    );
}
