using V3RII.Domain.Common;

namespace V3RII.Domain.Entities;

public sealed class MailOutboxItem : AuditableEntity
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
}
