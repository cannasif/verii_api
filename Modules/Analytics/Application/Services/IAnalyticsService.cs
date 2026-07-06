using V3RII.Application.DTOs;

namespace V3RII.Application.Interfaces;

public interface IAnalyticsService
{
    Task TrackAsync(TrackChatEventRequestDto request, CancellationToken cancellationToken = default);
    Task<AnalyticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
