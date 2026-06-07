using System;

namespace Trailblazers.Backend.Core.Domain.Entities
{
    public class ExamResult
    {
        public Guid Id { get; private set; }
        public Guid SessionId { get; private set; }
        public ExamSession? Session { get; private set; } // navigation property
        public string CandidateId { get; private set; } // StudentEmail
        public int TotalScore { get; private set; }
        public DateTimeOffset CompletedAt { get; private set; }

        private ExamResult()
        {
            CandidateId = string.Empty;
            Session = null!;
        }

        public ExamResult(Guid id, Guid sessionId, string candidateId, int totalScore, DateTimeOffset completedAt)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));

            if (sessionId == Guid.Empty)
                throw new ArgumentException("SessionId cannot be empty.", nameof(sessionId));

            if (string.IsNullOrWhiteSpace(candidateId))
                throw new ArgumentException("CandidateId is required.", nameof(candidateId));

            if (totalScore < 0)
                throw new ArgumentOutOfRangeException(nameof(totalScore), "Total score cannot be negative.");

            Id = id;
            SessionId = sessionId;
            CandidateId = candidateId.Trim().ToLowerInvariant();
            TotalScore = totalScore;
            CompletedAt = completedAt;
        }
    }
}
