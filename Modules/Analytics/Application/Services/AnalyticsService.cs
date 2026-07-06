using Microsoft.EntityFrameworkCore;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Domain.Entities;
using V3RII.Infrastructure.Persistence;

namespace V3RII.Infrastructure.Services;

public sealed class AnalyticsService(V3RiiDbContext dbContext) : IAnalyticsService
{
    public async Task TrackAsync(TrackChatEventRequestDto request, CancellationToken cancellationToken = default)
    {
        dbContext.ChatAnalyticsEvents.Add(new ChatAnalyticsEvent
        {
            Product = request.Product,
            EventType = request.EventType.Trim(),
            Intent = string.IsNullOrWhiteSpace(request.Intent) ? null : request.Intent.Trim(),
            SessionId = string.IsNullOrWhiteSpace(request.SessionId) ? null : request.SessionId.Trim(),
            MetadataJson = request.MetadataJson
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AnalyticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-30);
        var query = dbContext.ChatAnalyticsEvents.AsNoTracking().Where(x => x.CreatedAt >= since);

        var byProduct = await query
            .Where(x => x.Product != null)
            .GroupBy(x => x.Product!.Value.ToString())
            .Select(x => new AnalyticsBucketDto(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var byIntent = await query
            .Where(x => x.Intent != null && x.Intent != "")
            .GroupBy(x => x.Intent!)
            .Select(x => new AnalyticsBucketDto(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync(cancellationToken);

        var byEventType = await query
            .GroupBy(x => x.EventType)
            .Select(x => new AnalyticsBucketDto(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var chatStartedCount = await query.CountAsync(x => x.EventType == "chat_started", cancellationToken);
        var ticketCreatedCount = await query.CountAsync(x => x.EventType == "ticket_created", cancellationToken);
        var dropOffCount = await query.CountAsync(x => x.EventType == "drop_off", cancellationToken);
        var conversionRate = chatStartedCount == 0 ? 0 : Math.Round(ticketCreatedCount * 100m / chatStartedCount, 2);

        return new AnalyticsSummaryDto(byProduct, byIntent, byEventType, chatStartedCount, ticketCreatedCount, dropOffCount, conversionRate);
    }
}
