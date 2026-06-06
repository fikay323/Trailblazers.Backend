namespace Trailblazers.Backend.Core.Application.DTOs
{
    public record QuestionDto(
        Guid Id,
        string Subject,
        string QuestionText,
        Dictionary<char, string> Options,
        string? ImageUrl,
        string? ComprehensionPassage
    );
}
