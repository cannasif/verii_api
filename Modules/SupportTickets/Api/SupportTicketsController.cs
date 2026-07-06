using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using V3RII.Api.Authorization;
using V3RII.Application.Common;
using V3RII.Application.Common.Security;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;

namespace V3RII.Api.Controllers;

[ApiController]
[Route("api/support/tickets")]
public sealed class SupportTicketsController(ISupportTicketService supportTicketService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("public-chatbot")]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> Create(CreateSupportTicketRequestDto request, CancellationToken cancellationToken)
    {
        var result = await supportTicketService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<SupportTicketDto>.Ok(result, "Destek talebi oluşturuldu."));
    }

    [HttpGet]
    [PermissionAuthorize(PermissionCodes.SupportTicketsRead)]
    public async Task<ActionResult<ApiResponse<PagedResult<SupportTicketDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? product = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await supportTicketService.GetAllAsync(page, pageSize, status, product, search, cancellationToken);
        return Ok(ApiResponse<PagedResult<SupportTicketDto>>.Ok(result));
    }

    [HttpGet("dashboard")]
    [PermissionAuthorize(PermissionCodes.SupportTicketsRead)]
    public async Task<ActionResult<ApiResponse<SupportDashboardDto>>> Dashboard(CancellationToken cancellationToken)
    {
        var result = await supportTicketService.GetDashboardAsync(cancellationToken);
        return Ok(ApiResponse<SupportDashboardDto>.Ok(result));
    }

    [HttpPatch("{id:long}/status")]
    [PermissionAuthorize(PermissionCodes.SupportTicketsManage)]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> UpdateStatus(long id, UpdateSupportTicketStatusRequestDto request, CancellationToken cancellationToken)
    {
        var result = await supportTicketService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SupportTicketDto>.Ok(result, "Destek talebi güncellendi."));
    }
}
