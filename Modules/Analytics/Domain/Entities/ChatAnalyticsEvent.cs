using V3RII.Domain.Common;
using V3RII.Domain.Enums;

namespace V3RII.Domain.Entities;

public sealed class ChatAnalyticsEvent : AuditableEntity
{
    public ProductKey? Product { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string? SessionId { get; set; }
    public string? MetadataJson { get; set; }
}
