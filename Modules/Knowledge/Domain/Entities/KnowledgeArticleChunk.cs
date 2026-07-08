using V3RII.Domain.Common;
using V3RII.Domain.Enums;

namespace V3RII.Domain.Entities;

public sealed class KnowledgeArticleChunk : AuditableEntity
{
    public long KnowledgeArticleId { get; set; }
    public KnowledgeArticle? KnowledgeArticle { get; set; }
    public ProductKey Product { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int TokenEstimate { get; set; }
    public bool IsPublished { get; set; } = true;
}
