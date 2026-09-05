using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trailblazers.Backend.Core.Application.DTOs;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Application.Exceptions;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Core.Application.Features.Exams.GetExamResult
{
    public record GetExamResultQuery(Guid SessionId) : IRequest<ExamResultResponseDto>;

    public class GetExamResultQueryHandler(
        IExamSessionRepository sessionRepository,
        IExamQuestionRepository questionRepository)
        : IRequestHandler<GetExamResultQuery, ExamResultResponseDto>
    {
        public async Task<ExamResultResponseDto> Handle(GetExamResultQuery request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var result = await sessionRepository.GetResultBySessionIdAsync(request.SessionId);
            if (result == null || result.Session == null)
            {
                throw new NotFoundException($"Exam result for session {request.SessionId} was not found.");
            }

            if (!result.Session.IsCompleted)
            {
                throw new NotFoundException($"Exam session {request.SessionId} is not completed.");
            }

            var session = result.Session;
            var answers = session.Answers ?? [];
            var answersMap = answers.ToDictionary(a => a.QuestionId, a => a.SelectedOption);

            var questionIds = (session.AssignedQuestionIds != null && session.AssignedQuestionIds.Count > 0)
                ? session.AssignedQuestionIds
                : answers.Select(a => a.QuestionId).ToList();

            var questions = (await questionRepository.GetByIdsAsync(questionIds)).ToList();

            // Map review questions (preserving order of assigned questions)
            var reviewQuestions = new List<ReviewQuestionDto>();
            foreach (var question in questions)
            {
                answersMap.TryGetValue(question.Id, out var selectedChar);
                string? selectedStr = (selectedChar == '\0' || selectedChar == '-') ? null : selectedChar.ToString();

                reviewQuestions.Add(new ReviewQuestionDto(
                    Id: question.Id,
                    Subject: question.Subject.ToString(),
                    QuestionText: question.QuestionText,
                    Options: question.Options,
                    SelectedOption: selectedStr,
                    CorrectOption: question.CorrectOption.ToString(),
                    ComprehensionPassage: question.ComprehensionPassage
                ));
            }

            // Map subject performance
            var subjectPerformance = reviewQuestions
                .GroupBy(rq => rq.Subject)
                .Select(g => new SubjectPerformanceDto(
                    Subject: g.Key,
                    Score: g.Count(rq => string.Equals(rq.SelectedOption, rq.CorrectOption, StringComparison.OrdinalIgnoreCase)),
                    TotalQuestions: g.Count()
                ))
                .ToList();

            double elapsedTimeSeconds = (result.CompletedAt - session.StartTime).TotalSeconds;

            int totalQuestionsCount = (session.AssignedQuestionIds != null && session.AssignedQuestionIds.Count > 0)
                ? session.AssignedQuestionIds.Count
                : questions.Count;

            return new ExamResultResponseDto(
                SessionId: session.Id,
                StudentEmail: session.StudentEmail,
                TargetYear: session.TargetYear,
                OverallScore: result.TotalScore,
                TotalQuestions: totalQuestionsCount,
                CompletedAt: result.CompletedAt,
                ElapsedTimeSeconds: elapsedTimeSeconds,
                SubjectPerformance: subjectPerformance,
                ReviewQuestions: reviewQuestions
            );
        }
    }
}
