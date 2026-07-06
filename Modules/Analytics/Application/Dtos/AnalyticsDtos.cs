using V3RII.Domain.Enums;

namespace V3RII.Application.DTOs;

public sealed record TrackChatEventRequestDto(ProductKey? Product, string EventType, string? Intent, string? SessionId, string? MetadataJson);
public sealed record AnalyticsSummaryDto(
    IReadOnlyList<AnalyticsBucketDto> ByProduct,
    IReadOnlyList<AnalyticsBucketDto> ByIntent,
    IReadOnlyList<AnalyticsBucketDto> ByEventType,
    int ChatStartedCount,
    int TicketCreatedCount,
    int DropOffCount,
    decimal TicketConversionRate);
public sealed record AnalyticsBucketDto(string Key, int Count);
