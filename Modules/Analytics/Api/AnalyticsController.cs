using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using V3RII.Api.Authorization;
using V3RII.Application.Common;
using V3RII.Application.Common.Security;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;

namespace V3RII.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public sealed class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpPost("events")]
    [EnableRateLimiting("public-chatbot")]
    public async Task<ActionResult<ApiResponse>> Track(TrackChatEventRequestDto request, CancellationToken cancellationToken)
    {
        await analyticsService.TrackAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok("Analitik olayı kaydedildi."));
    }

    [HttpGet("summary")]
    [PermissionAuthorize(PermissionCodes.AnalyticsRead)]
    public async Task<ActionResult<ApiResponse<AnalyticsSummaryDto>>> Summary(CancellationToken cancellationToken)
    {
        var result = await analyticsService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<AnalyticsSummaryDto>.Ok(result));
    }

    [HttpGet("unanswered-questions")]
    [PermissionAuthorize(PermissionCodes.AnalyticsRead)]
    public async Task<ActionResult<ApiResponse<PagedResult<UnansweredQuestionDto>>>> UnansweredQuestions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? product = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await analyticsService.GetUnansweredQuestionsAsync(page, pageSize, product, search, cancellationToken);
        return Ok(ApiResponse<PagedResult<UnansweredQuestionDto>>.Ok(result));
    }

    [HttpGet("answer-feedback")]
    [PermissionAuthorize(PermissionCodes.AnalyticsRead)]
    public async Task<ActionResult<ApiResponse<PagedResult<AnswerFeedbackDto>>>> AnswerFeedback(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? product = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await analyticsService.GetAnswerFeedbackAsync(page, pageSize, product, search, cancellationToken);
        return Ok(ApiResponse<PagedResult<AnswerFeedbackDto>>.Ok(result));
    }
}
