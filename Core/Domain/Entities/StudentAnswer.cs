namespace Trailblazers.Backend.Core.Domain.Entities
{
    public class StudentAnswer
    {
        public Guid QuestionId { get; private set; }
        public char SelectedOption { get; private set; }

        private StudentAnswer()
        {
        }

        public StudentAnswer(Guid questionId, char selectedOption)
        {
            if (questionId == Guid.Empty)
                throw new ArgumentException("QuestionId cannot be empty.", nameof(questionId));

            QuestionId = questionId;
            SelectedOption = selectedOption;
        }
    }
}
