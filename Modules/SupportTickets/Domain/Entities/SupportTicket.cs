using V3RII.Domain.Common;
using V3RII.Domain.Enums;

namespace V3RII.Domain.Entities;

public sealed class SupportTicket : AuditableEntity
{
    public string TicketNo { get; set; } = string.Empty;
    public ProductKey Product { get; set; }
    public string Intent { get; set; } = string.Empty;
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.New;
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? TranscriptJson { get; set; }
    public string? AssignedToEmail { get; set; }
    public bool RequiresHandoff { get; set; }
    public string? HandoffReason { get; set; }
    public string Source { get; set; } = "website-chatbot";
    public DateTimeOffset? LastNotificationAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}
