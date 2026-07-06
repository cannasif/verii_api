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
}
