using V3RII.Domain.Enums;

namespace V3RII.Application.DTOs;

public sealed record CreateSupportTicketRequestDto(
    ProductKey Product,
    string Intent,
    string CustomerName,
    string CustomerEmail,
    string? CompanyName,
    string Details,
    string? TranscriptJson,
    bool RequiresHandoff = false,
    string? HandoffReason = null,
    string Source = "website-chatbot");

public sealed record UpdateSupportTicketStatusRequestDto(SupportTicketStatus Status, string? AssignedToEmail);

public sealed record SupportTicketDto(
    long Id,
    string TicketNo,
    ProductKey Product,
    string Intent,
    SupportTicketStatus Status,
    SupportTicketPriority Priority,
    string CustomerName,
    string CustomerEmail,
    string? CompanyName,
    string Details,
    bool RequiresHandoff,
    string? HandoffReason,
    string Source,
    string? AssignedToEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

public sealed record SupportDashboardDto(
    int OpenTicketCount,
    int UrgentTicketCount,
    int HandoffQueueCount,
    int WaitingCustomerCount,
    int ResolvedLast7Days,
    IReadOnlyList<SupportTicketDto> LatestTickets);
