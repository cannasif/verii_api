using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using V3RII.Application.Common;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;

namespace V3RII.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(IChatAnswerService chatAnswerService) : ControllerBase
{
    [HttpPost("answer")]
    [EnableRateLimiting("public-chatbot")]
    public async Task<ActionResult<ApiResponse<ChatAnswerDto>>> Answer(ChatAnswerRequestDto request, CancellationToken cancellationToken)
    {
        var result = await chatAnswerService.AnswerAsync(request, cancellationToken);
        return Ok(ApiResponse<ChatAnswerDto>.Ok(result));
    }
}
