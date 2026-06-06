namespace Trailblazers.Backend.Core.Domain.Entities
{
    public class ExamSession
    {
        public Guid Id { get; private set; }
        public string StudentEmail { get; private set; }
        public int TargetYear { get; private set; }
        public DateTimeOffset StartTime { get; private set; }
        public DateTimeOffset EndTime { get; private set; }
        public bool IsCompleted { get; private set; }
        public int? Score { get; private set; }
        public List<StudentAnswer> Answers { get; private set; }

        private ExamSession()
        {
            StudentEmail = string.Empty;
            Answers = [];
        }

        public ExamSession(
            Guid id,
            string studentEmail,
            int targetYear,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            List<StudentAnswer>? answers = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id cannot be empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(studentEmail))
                throw new ArgumentException("StudentEmail is required.", nameof(studentEmail));

            if (endTime <= startTime)
                throw new ArgumentException("EndTime must be after StartTime.", nameof(endTime));

            Id = id;
            StudentEmail = studentEmail.Trim().ToLowerInvariant();
            TargetYear = targetYear;
            StartTime = startTime;
            EndTime = endTime;
            IsCompleted = false;
            Score = null;
            Answers = answers ?? [];
        }

        public void CompleteSession(int calculatedScore)
        {
            if (IsCompleted)
                throw new InvalidOperationException("The session has already been completed.");

            if (calculatedScore < 0)
                throw new ArgumentOutOfRangeException(nameof(calculatedScore), "Calculated score cannot be negative.");

            Score = calculatedScore;
            IsCompleted = true;
        }
    }
}
