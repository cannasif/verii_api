using V3RII.Application.DTOs;
using V3RII.Application.Common;

namespace V3RII.Application.Interfaces;

public interface IAnalyticsService
{
    Task TrackAsync(TrackChatEventRequestDto request, CancellationToken cancellationToken = default);
    Task<AnalyticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<UnansweredQuestionDto>> GetUnansweredQuestionsAsync(int page, int pageSize, string? product, string? search, CancellationToken cancellationToken = default);
    Task<PagedResult<AnswerFeedbackDto>> GetAnswerFeedbackAsync(int page, int pageSize, string? product, string? search, CancellationToken cancellationToken = default);
}
