namespace V3RII.Application.DTOs;

public sealed record MailOutboxItemDto(
    long Id,
    string To,
    string Subject,
    int AttemptCount,
    DateTimeOffset? SentAt,
    DateTimeOffset? NextAttemptAt,
    string? LastError,
    DateTimeOffset CreatedAt);

public sealed record MailOutboxSummaryDto(
    int PendingCount,
    int FailedCount,
    int SentLast24HoursCount,
    int RetryReadyCount);
