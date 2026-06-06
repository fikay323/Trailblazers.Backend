using MediatR;
using Trailblazers.Backend.Core.Application.DTOs;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Application.Exceptions;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Core.Application.Features.Exams.SubmitExam
{
    public record SubmitExamCommand(
        Guid SessionId,
        Dictionary<Guid, char> StudentAnswers
    ) : IRequest<ExamSubmitResponseDto>;

    public class SubmitExamCommandHandler(
        IExamSessionRepository sessionRepository,
        IExamQuestionRepository questionRepository)
        : IRequestHandler<SubmitExamCommand, ExamSubmitResponseDto>
    {
        public async Task<ExamSubmitResponseDto> Handle(SubmitExamCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // 1. Fetch the ExamSession
            var session = await sessionRepository.GetByIdAsync(request.SessionId);
            if (session == null)
            {
                throw new ValidationException($"Exam session with ID {request.SessionId} was not found.");
            }

            if (session.IsCompleted)
            {
                throw new ValidationException("This exam session has already been completed and submitted.");
            }

            // 2. Optional Time Validation (allowing a 5-minute grace period for network latency)
            var gracePeriod = TimeSpan.FromMinutes(5);
            if (DateTimeOffset.UtcNow > session.EndTime.Add(gracePeriod))
            {
                throw new ValidationException("The submission window for this session has closed.");
            }

            // 3. Fetch the relevant ExamQuestion entities based on the question IDs submitted
            var questionIds = request.StudentAnswers.Keys.ToList();
            var questions = (await questionRepository.GetByIdsAsync(questionIds)).ToList();

            // 4. Compare the student's selected options against CorrectOption to calculate the score
            int calculatedScore = 0;
            foreach (var question in questions)
            {
                if (request.StudentAnswers.TryGetValue(question.Id, out var selectedOption))
                {
                    // Case-insensitive comparison is safer for option characters
                    if (char.ToUpperInvariant(selectedOption) == char.ToUpperInvariant(question.CorrectOption))
                    {
                        calculatedScore++;
                    }
                }
            }

            // 5. Populate student answers in the session
            session.Answers.Clear();
            foreach (var kvp in request.StudentAnswers)
            {
                session.Answers.Add(new StudentAnswer(kvp.Key, kvp.Value));
            }

            // 6. Complete the session
            session.CompleteSession(calculatedScore);

            // 7. Update the session via the repository
            await sessionRepository.UpdateAsync(session);

            return new ExamSubmitResponseDto(
                Score: calculatedScore,
                TotalQuestions: questions.Count,
                CompletedAt: DateTimeOffset.UtcNow
            );
        }
    }
}
