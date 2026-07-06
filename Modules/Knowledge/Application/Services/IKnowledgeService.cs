using V3RII.Application.DTOs;
using V3RII.Domain.Enums;

namespace V3RII.Application.Interfaces;

public interface IKnowledgeService
{
    Task<IReadOnlyList<KnowledgeArticleDto>> SearchAsync(ProductKey? product, string? query, CancellationToken cancellationToken = default);
    Task<KnowledgeArticleDto> UpsertAsync(long? id, UpsertKnowledgeArticleRequestDto request, CancellationToken cancellationToken = default);
}
