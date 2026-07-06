using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using V3RII.Api.Authorization;
using V3RII.Application.Common;
using V3RII.Application.Common.Security;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Domain.Enums;

namespace V3RII.Api.Controllers;

[ApiController]
[Route("api/knowledge")]
public sealed class KnowledgeController(IKnowledgeService knowledgeService) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("public-chatbot")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KnowledgeArticleDto>>>> Search([FromQuery] ProductKey? product, [FromQuery] string? query, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.SearchAsync(product, query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<KnowledgeArticleDto>>.Ok(result));
    }

    [HttpPost]
    [PermissionAuthorize(PermissionCodes.KnowledgeManage)]
    public async Task<ActionResult<ApiResponse<KnowledgeArticleDto>>> Create(UpsertKnowledgeArticleRequestDto request, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.UpsertAsync(null, request, cancellationToken);
        return Ok(ApiResponse<KnowledgeArticleDto>.Ok(result, "Bilgi tabanı kaydı oluşturuldu."));
    }

    [HttpPut("{id:long}")]
    [PermissionAuthorize(PermissionCodes.KnowledgeManage)]
    public async Task<ActionResult<ApiResponse<KnowledgeArticleDto>>> Update(long id, UpsertKnowledgeArticleRequestDto request, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.UpsertAsync(id, request, cancellationToken);
        return Ok(ApiResponse<KnowledgeArticleDto>.Ok(result, "Bilgi tabanı kaydı güncellendi."));
    }
}
