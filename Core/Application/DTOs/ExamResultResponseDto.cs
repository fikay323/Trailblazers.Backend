using System;
using System.Collections.Generic;

namespace Trailblazers.Backend.Core.Application.DTOs
{
    public record ExamResultResponseDto(
        Guid SessionId,
        string StudentEmail,
        int TargetYear,
        int OverallScore,
        int TotalQuestions,
        DateTimeOffset CompletedAt,
        double ElapsedTimeSeconds,
        List<SubjectPerformanceDto> SubjectPerformance,
        List<ReviewQuestionDto> ReviewQuestions
    );

    public record SubjectPerformanceDto(
        string Subject,
        int Score,
        int TotalQuestions
    );

    public record ReviewQuestionDto(
        Guid Id,
        string Subject,
        string QuestionText,
        Dictionary<char, string> Options,
        string? SelectedOption,
        string CorrectOption,
        string? ComprehensionPassage
    );
}
