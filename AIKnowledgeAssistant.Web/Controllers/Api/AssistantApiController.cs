using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace AIKnowledgeAssistant.Web.Controllers.Api;

[ApiController]
[Route("api")]
[EnableCors("AllowEmbed")]
public class AssistantApiController : ControllerBase
{
    private readonly IQuestionPipelineService _pipelineService;

    public AssistantApiController(IQuestionPipelineService pipelineService)
    {
        _pipelineService = pipelineService;
    }

    public class AskApiRequest
    {
        public string? Token { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskApiRequest request, [FromQuery] string? token)
    {
        // 1. Resolve Assistant Token from Header, Query, or Body
        var assistantToken = Request.Headers["X-Assistant-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(assistantToken))
        {
            assistantToken = token;
        }
        if (string.IsNullOrWhiteSpace(assistantToken))
        {
            assistantToken = request.Token;
        }

        if (string.IsNullOrWhiteSpace(assistantToken))
        {
            return Unauthorized(new
            {
                success = false,
                error = "Assistant token is missing. Please provide it via the 'X-Assistant-Token' header, '?token=' query parameter, or JSON body."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new
            {
                success = false,
                error = "Question cannot be empty."
            });
        }

        // 2. Execute Full AI & Decision Pipeline
        var result = await _pipelineService.ProcessQuestionAsync(
            assistantToken.Trim(), request.Question.Trim(), request.SessionId);

        if (!result.IsSuccess || result.Value == null)
        {
            return StatusCode(result.StatusCode, new
            {
                success = false,
                error = result.Error ?? "Failed to process question."
            });
        }

        return Ok(new
        {
            success = true,
            data = result.Value
        });
    }
}
