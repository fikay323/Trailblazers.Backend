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

            // Handle idempotency: if already submitted, return the existing score rather than failing
            if (session.IsCompleted)
            {
                var existingResult = await sessionRepository.GetResultBySessionIdAsync(session.Id);
                if (existingResult != null)
                {
                    var totalAssigned = session.AssignedQuestionIds.Count > 0
                        ? session.AssignedQuestionIds.Count
                        : session.Answers.Count;

                    return new ExamSubmitResponseDto(
                        Score: existingResult.TotalScore,
                        TotalQuestions: totalAssigned,
                        CompletedAt: existingResult.CompletedAt
                    );
                }
                throw new ValidationException("This exam session has already been completed and submitted.");
            }

            // 2. Optional Time Validation (allowing a 5-minute grace period for network latency)
            var gracePeriod = TimeSpan.FromMinutes(5);
            if (DateTimeOffset.UtcNow > session.EndTime.Add(gracePeriod))
            {
                throw new ValidationException("The submission window for this session has closed.");
            }

            // 3. Determine questions to grade: bind strictly to assigned questions to prevent spoofing
            var assignedIds = session.AssignedQuestionIds.Count > 0
                ? session.AssignedQuestionIds
                : request.StudentAnswers.Keys.ToList();

            var questions = (await questionRepository.GetByIdsAsync(assignedIds)).ToList();
            var validSubmittedAnswers = request.StudentAnswers
                .Where(kvp => assignedIds.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // 4. Compare the student's selected options against CorrectOption to calculate the score
            int calculatedScore = 0;
            foreach (var question in questions)
            {
                if (validSubmittedAnswers.TryGetValue(question.Id, out var selectedOption))
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
            foreach (var kvp in validSubmittedAnswers)
            {
                session.Answers.Add(new StudentAnswer(kvp.Key, kvp.Value));
            }

            // 6. Instantiate the new ExamResult entity
            var examResult = new ExamResult(
                id: Guid.NewGuid(),
                sessionId: session.Id,
                candidateId: session.StudentEmail,
                totalScore: calculatedScore,
                completedAt: DateTimeOffset.UtcNow
            );

            // 7. Add result to the context and mark session as Completed
            await sessionRepository.AddResultAsync(examResult);
            session.Complete();

            // 8. Save the changes via the repository
            await sessionRepository.UpdateAsync(session);

            int totalDenominator = assignedIds.Count > 0 ? assignedIds.Count : questions.Count;

            return new ExamSubmitResponseDto(
                Score: calculatedScore,
                TotalQuestions: totalDenominator,
                CompletedAt: examResult.CompletedAt
            );
        }
    }
}
