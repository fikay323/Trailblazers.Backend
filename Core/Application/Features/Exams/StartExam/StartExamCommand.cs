using MediatR;
using Trailblazers.Backend.Core.Application.DTOs;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Application.Exceptions;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Core.Application.Features.Exams.StartExam
{
    public record StartExamCommand(
        string StudentEmail,
        int Year,
        List<string> Subjects
    ) : IRequest<ExamStartResponseDto>;

    public class StartExamCommandHandler(
        IExamQuestionRepository questionRepository,
        IExamSessionRepository sessionRepository)
        : IRequestHandler<StartExamCommand, ExamStartResponseDto>
    {
        public async Task<ExamStartResponseDto> Handle(StartExamCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // 1. Fetch questions for the specified year and subjects
            var questionsList =
                (await questionRepository.GetQuestionsByYearAndSubjectsAsync(request.Year, request.Subjects)).ToList();

            if (questionsList.Count == 0)
            {
                throw new NotFoundException(
                    $"No exam questions found for year {request.Year} and subjects: {string.Join(", ", request.Subjects)}");
            }

            // 2. Create a new ExamSession entity
            var sessionId = Guid.NewGuid();
            var startTime = DateTimeOffset.UtcNow;
            var endTime = startTime.AddMinutes(120);

            var session = new ExamSession(
                sessionId,
                request.StudentEmail,
                request.Year,
                startTime,
                endTime
            );

            // 3. Save the session
            await sessionRepository.AddAsync(session);

            // 4. Map questions to QuestionDto (stripping CorrectOption)
            var questionDtos = questionsList.Select(q => new QuestionDto(
                q.Id,
                q.Subject,
                q.QuestionText,
                q.Options,
                q.ImageUrl,
                q.ComprehensionPassage
            )).ToList();

            return new ExamStartResponseDto(session.Id, session.EndTime, questionDtos);
        }
    }
}
