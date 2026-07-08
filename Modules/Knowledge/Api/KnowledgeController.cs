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
public sealed class KnowledgeController(IKnowledgeService knowledgeService, IKnowledgeDocumentTextExtractor documentTextExtractor) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("public-chatbot")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KnowledgeArticleDto>>>> Search([FromQuery] ProductKey? product, [FromQuery] string? query, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.SearchAsync(product, query, includeUnpublished: false, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<KnowledgeArticleDto>>.Ok(result));
    }

    [HttpGet("manage")]
    [PermissionAuthorize(PermissionCodes.KnowledgeRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KnowledgeArticleDto>>>> Manage([FromQuery] ProductKey? product, [FromQuery] string? query, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.SearchAsync(product, query, includeUnpublished: true, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<KnowledgeArticleDto>>.Ok(result));
    }

    [HttpGet("chunks")]
    [EnableRateLimiting("public-chatbot")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KnowledgeChunkDto>>>> SearchChunks([FromQuery] ProductKey? product, [FromQuery] string? query, [FromQuery] int take, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.SearchChunksAsync(product, query, includeUnpublished: false, take: take <= 0 ? 20 : take, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<KnowledgeChunkDto>>.Ok(result));
    }

    [HttpGet("chunks/manage")]
    [PermissionAuthorize(PermissionCodes.KnowledgeRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KnowledgeChunkDto>>>> ManageChunks([FromQuery] ProductKey? product, [FromQuery] string? query, [FromQuery] int take, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.SearchChunksAsync(product, query, includeUnpublished: true, take: take <= 0 ? 50 : take, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<KnowledgeChunkDto>>.Ok(result));
    }

    [HttpPost("chunks/rebuild")]
    [PermissionAuthorize(PermissionCodes.KnowledgeManage)]
    public async Task<ActionResult<ApiResponse<KnowledgeChunkRebuildResultDto>>> RebuildChunks(CancellationToken cancellationToken)
    {
        var result = await knowledgeService.RebuildChunksAsync(cancellationToken);
        return Ok(ApiResponse<KnowledgeChunkRebuildResultDto>.Ok(result, "RAG bilgi parçaları yeniden oluşturuldu."));
    }

    [HttpPost("import")]
    [RequestSizeLimit(2_500_000)]
    [PermissionAuthorize(PermissionCodes.KnowledgeManage)]
    public async Task<ActionResult<ApiResponse<ImportKnowledgeDocumentResultDto>>> Import(
        [FromForm] ProductKey product,
        [FromForm] string? title,
        [FromForm] string? tags,
        [FromForm] bool isPublished,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Doküman dosyası boş."));
        }

        if (file.Length > 2_000_000)
        {
            return BadRequest(ApiResponse.Fail("Doküman en fazla 2 MB olabilir."));
        }

        if (!documentTextExtractor.Supports(file.FileName))
        {
            return BadRequest(ApiResponse.Fail("Şimdilik sadece .txt, .md, .markdown, .pdf ve .docx dokümanları içe aktarılabilir."));
        }

        await using var stream = file.OpenReadStream();
        var content = await documentTextExtractor.ExtractAsync(stream, file.FileName, cancellationToken);
        var result = await knowledgeService.ImportDocumentAsync(product, title ?? string.Empty, content, tags ?? string.Empty, file.FileName, isPublished, cancellationToken);
        return Ok(ApiResponse<ImportKnowledgeDocumentResultDto>.Ok(result, "Doküman bilgi tabanına aktarıldı."));
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

    [HttpPost("{id:long}/update")]
    [PermissionAuthorize(PermissionCodes.KnowledgeManage)]
    public async Task<ActionResult<ApiResponse<KnowledgeArticleDto>>> UpdatePost(long id, UpsertKnowledgeArticleRequestDto request, CancellationToken cancellationToken)
    {
        var result = await knowledgeService.UpsertAsync(id, request, cancellationToken);
        return Ok(ApiResponse<KnowledgeArticleDto>.Ok(result, "Bilgi tabanı kaydı güncellendi."));
    }
}
