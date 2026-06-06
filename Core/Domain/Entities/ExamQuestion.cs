using System;
using System.Collections.Generic;

namespace Trailblazers.Backend.Core.Domain.Entities
{
    public class ExamQuestion
    {
        public Guid Id { get; private set; }
        public string Subject { get; private set; }
        public int ExamYear { get; private set; }
        public string QuestionText { get; private set; }
        public char CorrectOption { get; private set; }
        public Dictionary<char, string> Options { get; private set; }
        public string? ImageUrl { get; private set; }
        public string? ComprehensionPassage { get; private set; }

        // Parameterless constructor for EF Core if needed, though domain is pure C#
        private ExamQuestion()
        {
            Options = new Dictionary<char, string>();
            Subject = string.Empty;
            QuestionText = string.Empty;
        }

        public ExamQuestion(
            Guid id,
            string subject,
            int examYear,
            string questionText,
            char correctOption,
            Dictionary<char, string> options,
            string? imageUrl = null,
            string? comprehensionPassage = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject is required.", nameof(subject));

            if (string.IsNullOrWhiteSpace(questionText))
                throw new ArgumentException("Question text is required.", nameof(questionText));

            if (options == null || options.Count == 0)
                throw new ArgumentException("Options Dictionary cannot be null or empty.", nameof(options));

            Id = id;
            Subject = subject.Trim();
            ExamYear = examYear;
            QuestionText = questionText.Trim();
            CorrectOption = correctOption;
            Options = new Dictionary<char, string>(options);
            ImageUrl = imageUrl;
            ComprehensionPassage = comprehensionPassage;
        }
    }
}
