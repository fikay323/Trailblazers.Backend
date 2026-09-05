using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trailblazers.Backend.Core.Domain.Entities;
using Trailblazers.Backend.Core.Domain.Enums;
using Trailblazers.Backend.Infrastructure.Extensions;
using Trailblazers.Backend.Infrastructure.Persistence;
using Trailblazers.Backend.WebApi.Authentication;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/questions")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public class AdminQuestionsController(
        ApplicationDbContext dbContext,
        ILogger<AdminQuestionsController> logger) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetQuestions(
            [FromQuery] string? subject,
            [FromQuery] int? year,
            [FromQuery] string? examType,
            [FromQuery] string? searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = dbContext.ExamQuestions.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(subject))
            {
                var parsedSubject = ExamSubjectExtensions.ToExamSubject(subject);
                if (parsedSubject.HasValue)
                {
                    query = query.Where(q => q.Subject == parsedSubject.Value);
                }
            }

            if (year.HasValue)
            {
                query = query.Where(q => q.ExamYear == year.Value);
            }

            if (!string.IsNullOrWhiteSpace(examType))
            {
                query = query.Where(q => q.ExamType == examType.Trim().ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(q => q.QuestionText.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(q => q.ExamYear)
                .ThenBy(q => q.QuestionNumber)
                .Skip((Math.Max(1, pageNumber) - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new
                {
                    q.Id,
                    Subject = q.Subject.ToString(),
                    q.ExamYear,
                    q.QuestionText,
                    q.CorrectOption,
                    q.Options,
                    q.ExamType,
                    q.QuestionNumber,
                    q.ImageUrl,
                    q.ComprehensionPassage
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuestion(
            [FromBody] CreateQuestionRequestDto request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.QuestionText))
            {
                return BadRequest(new { error = "Subject and question text are required." });
            }

            var parsedSubject = ExamSubjectExtensions.ToExamSubject(request.Subject);
            if (!parsedSubject.HasValue)
            {
                return BadRequest(new { error = $"Invalid subject: {request.Subject}" });
            }

            if (request.Options == null || request.Options.Count < 2)
            {
                return BadRequest(new { error = "At least 2 options (A, B, ...) are required." });
            }

            // Generate a random unique integer for custom alocId
            var customAlocId = -(int)(DateTime.UtcNow.Ticks % 1_000_000_000);

            var question = new ExamQuestion(
                id: Guid.NewGuid(),
                subject: parsedSubject.Value,
                examYear: request.ExamYear <= 0 ? DateTime.UtcNow.Year : request.ExamYear,
                questionText: request.QuestionText.Trim(),
                correctOption: char.ToUpperInvariant(request.CorrectOption),
                options: request.Options,
                alocId: customAlocId,
                examType: string.IsNullOrWhiteSpace(request.ExamType) ? "custom" : request.ExamType.Trim().ToLowerInvariant(),
                questionNumber: request.QuestionNumber,
                imageUrl: request.ImageUrl?.Trim(),
                comprehensionPassage: request.ComprehensionPassage?.Trim()
            );

            await dbContext.ExamQuestions.AddAsync(question, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Custom question created with ID {Id} for subject {Subject}", question.Id, question.Subject);

            return CreatedAtAction(nameof(GetQuestions), new { id = question.Id }, new
            {
                question.Id,
                Subject = question.Subject.ToString(),
                question.ExamYear,
                question.QuestionText,
                question.CorrectOption,
                question.Options
            });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteQuestion(Guid id, CancellationToken cancellationToken)
        {
            var question = await dbContext.ExamQuestions.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
            if (question == null)
            {
                return NotFound(new { error = "Question not found." });
            }

            dbContext.ExamQuestions.Remove(question);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Question deleted successfully." });
        }
    }

    public class CreateQuestionRequestDto
    {
        public string Subject { get; set; } = string.Empty;
        public int ExamYear { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public char CorrectOption { get; set; }
        public Dictionary<char, string> Options { get; set; } = [];
        public string? ExamType { get; set; }
        public int? QuestionNumber { get; set; }
        public string? ImageUrl { get; set; }
        public string? ComprehensionPassage { get; set; }
    }
}
