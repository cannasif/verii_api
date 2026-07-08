using V3RII.Domain.Enums;

namespace V3RII.Application.DTOs;

public sealed record TrackChatEventRequestDto(ProductKey? Product, string EventType, string? Intent, string? SessionId, string? MetadataJson);
public sealed record UnansweredQuestionDto(
    long Id,
    ProductKey? Product,
    string? Intent,
    string? SessionId,
    string Question,
    string? Language,
    string? Reason,
    DateTimeOffset CreatedAt);
public sealed record AnswerFeedbackDto(
    long Id,
    ProductKey? Product,
    string? Intent,
    string? SessionId,
    string Rating,
    string Question,
    string Answer,
    string? Language,
    DateTimeOffset CreatedAt);
public sealed record AnalyticsSummaryDto(
    IReadOnlyList<AnalyticsBucketDto> ByProduct,
    IReadOnlyList<AnalyticsBucketDto> ByIntent,
    IReadOnlyList<AnalyticsBucketDto> ByEventType,
    int ChatStartedCount,
    int TicketCreatedCount,
    int DropOffCount,
    decimal TicketConversionRate);
public sealed record AnalyticsBucketDto(string Key, int Count);
