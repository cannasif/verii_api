using V3RII.Application.DTOs;
using V3RII.Domain.Enums;

namespace V3RII.Application.Interfaces;

public interface IKnowledgeService
{
    Task<IReadOnlyList<KnowledgeArticleDto>> SearchAsync(ProductKey? product, string? query, bool includeUnpublished = false, CancellationToken cancellationToken = default);
    Task<KnowledgeArticleDto> UpsertAsync(long? id, UpsertKnowledgeArticleRequestDto request, CancellationToken cancellationToken = default);
    Task<ImportKnowledgeDocumentResultDto> ImportDocumentAsync(ProductKey product, string title, string content, string tags, string fileName, bool isPublished, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeChunkDto>> SearchChunksAsync(ProductKey? product, string? query, bool includeUnpublished = false, int take = 20, CancellationToken cancellationToken = default);
    Task<KnowledgeChunkRebuildResultDto> RebuildChunksAsync(CancellationToken cancellationToken = default);
}
