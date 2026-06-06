using System.Collections.Concurrent;
using Trailblazers.Backend.Core.Application.Interfaces;
using Trailblazers.Backend.Core.Domain.Entities;

namespace Trailblazers.Backend.Infrastructure.Persistence.Repositories
{
    public class ExamQuestionRepository : IExamQuestionRepository
    {
        private static readonly ConcurrentDictionary<Guid, ExamQuestion> CachedQuestions = new();

        public Task<IEnumerable<ExamQuestion>> GetQuestionsByYearAndSubjectsAsync(int year,
            IEnumerable<string> subjects)
        {
            var result = new List<ExamQuestion>();

            foreach (var subject in subjects)
            {
                // Generate 60 questions for English, 40 for others
                int questionCount = subject.Equals("Use of English", StringComparison.OrdinalIgnoreCase) ? 60 : 40;

                for (int i = 1; i <= questionCount; i++)
                {
                    // Generate deterministic GUID based on subject, year and index
                    var guidBytes = new byte[16];
                    var subjectHash = Math.Abs(subject.GetHashCode());
                    BitConverter.GetBytes(year).CopyTo(guidBytes, 0);
                    BitConverter.GetBytes(subjectHash).CopyTo(guidBytes, 4);
                    BitConverter.GetBytes(i).CopyTo(guidBytes, 8);

                    var questionId = new Guid(guidBytes);

                    var options = new Dictionary<char, string>
                    {
                        { 'A', $"Option A for {subject} Question {i}" },
                        { 'B', $"Option B for {subject} Question {i}" },
                        { 'C', $"Option C for {subject} Question {i}" },
                        { 'D', $"Option D for {subject} Question {i}" }
                    };

                    // Pick a deterministic correct option
                    char correctOption = (char)('A' + ((subjectHash + i) % 4));

                    string? passage = null;
                    if (subject.Equals("Use of English", StringComparison.OrdinalIgnoreCase) && i <= 15)
                    {
                        passage =
                            "Read the passage carefully and answer the questions that follow.\n\nTrailblazers Academy has pioneered mock testing solutions for over a decade, training students to achieve excellence in their UTME examinations through rigorous simulated environments.";
                    }

                    var question = new ExamQuestion(
                        questionId,
                        subject,
                        year,
                        $"This is the simulated test question #{i} for the subject {subject} in the year {year}. What is the correct option?",
                        correctOption,
                        options,
                        imageUrl: null,
                        comprehensionPassage: passage
                    );

                    CachedQuestions[questionId] = question;
                    result.Add(question);
                }
            }

            return Task.FromResult<IEnumerable<ExamQuestion>>(result);
        }

        public Task<IEnumerable<ExamQuestion>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var result = new List<ExamQuestion>();
            foreach (var id in ids)
            {
                if (CachedQuestions.TryGetValue(id, out var question))
                {
                    result.Add(question);
                }
                else
                {
                    // Generate a fallback question if not cached (fallback for testing)
                    var fallbackOptions = new Dictionary<char, string>
                    {
                        { 'A', "Default Option A" },
                        { 'B', "Default Option B" },
                        { 'C', "Default Option C" },
                        { 'D', "Default Option D" }
                    };
                    var fallback = new ExamQuestion(id, "General Paper", 2024,
                        "Fallback test question. What is the answer?", 'A', fallbackOptions);
                    CachedQuestions[id] = fallback;
                    result.Add(fallback);
                }
            }

            return Task.FromResult<IEnumerable<ExamQuestion>>(result);
        }
    }
}
