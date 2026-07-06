using V3RII.Domain.Common;
using V3RII.Domain.Enums;

namespace V3RII.Domain.Entities;

public sealed class KnowledgeArticle : AuditableEntity
{
    public ProductKey Product { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
}
