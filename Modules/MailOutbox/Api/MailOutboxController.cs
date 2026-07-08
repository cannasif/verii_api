using Microsoft.AspNetCore.Mvc;
using V3RII.Api.Authorization;
using V3RII.Application.Common;
using V3RII.Application.Common.Security;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;

namespace V3RII.Api.Controllers;

[ApiController]
[Route("api/mail-outbox")]
public sealed class MailOutboxController(IMailOutboxService mailOutboxService, IMailOutboxProcessor mailOutboxProcessor) : ControllerBase
{
    [HttpGet]
    [PermissionAuthorize(PermissionCodes.SupportTicketsManage)]
    public async Task<ActionResult<ApiResponse<PagedResult<MailOutboxItemDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mailOutboxService.GetAllAsync(page, pageSize, status, search, cancellationToken);
        return Ok(ApiResponse<PagedResult<MailOutboxItemDto>>.Ok(result));
    }

    [HttpGet("summary")]
    [PermissionAuthorize(PermissionCodes.SupportTicketsManage)]
    public async Task<ActionResult<ApiResponse<MailOutboxSummaryDto>>> Summary(CancellationToken cancellationToken)
    {
        var result = await mailOutboxService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<MailOutboxSummaryDto>.Ok(result));
    }

    [HttpPost("{id:long}/retry")]
    [PermissionAuthorize(PermissionCodes.SupportTicketsManage)]
    public async Task<ActionResult<ApiResponse<MailOutboxItemDto>>> Retry(long id, CancellationToken cancellationToken)
    {
        var result = await mailOutboxService.RetryAsync(id, cancellationToken);
        await mailOutboxProcessor.ProcessPendingAsync(cancellationToken);
        return Ok(ApiResponse<MailOutboxItemDto>.Ok(result, "Mail kuyruğu yeniden denemeye alındı."));
    }
}
