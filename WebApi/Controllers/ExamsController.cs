using MediatR;
using Microsoft.AspNetCore.Mvc;
using Trailblazers.Backend.Core.Application.Features.Exams.GetExamMetadata;
using Trailblazers.Backend.Core.Application.Features.Exams.StartExam;
using Trailblazers.Backend.Core.Application.Features.Exams.SubmitExam;

namespace Trailblazers.Backend.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamsController(IMediator mediator) : ControllerBase
    {
        [HttpGet("metadata")]
        public async Task<IActionResult> GetMetadata(CancellationToken cancellationToken)
        {
            try
            {
                var result = await mediator.Send(new GetExamMetadataQuery(), cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to load exam metadata.", details = ex.Message });
            }
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartExam([FromBody] StartExamRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var command = new StartExamCommand(request.StudentEmail, request.Year, request.Subjects);
                var result = await mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to initialize exam session.", details = ex.Message });
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitExam([FromBody] SubmitExamRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var command = new SubmitExamCommand(request.SessionId, request.StudentAnswers);
                var result = await mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to submit exam session.", details = ex.Message });
            }
        }
    }

    public class StartExamRequestDto
    {
        public string StudentEmail { get; set; } = string.Empty;
        public int Year { get; set; }
        public List<string> Subjects { get; set; } = [];
    }

    public class SubmitExamRequestDto
    {
        public Guid SessionId { get; set; }
        public Dictionary<Guid, char> StudentAnswers { get; set; } = [];
    }
}
