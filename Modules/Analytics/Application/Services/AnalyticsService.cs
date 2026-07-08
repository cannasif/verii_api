using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using V3RII.Application.Common;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Domain.Entities;
using V3RII.Domain.Enums;
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

    public async Task<PagedResult<UnansweredQuestionDto>> GetUnansweredQuestionsAsync(int page, int pageSize, string? product, string? search, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var query = dbContext.ChatAnalyticsEvents
            .AsNoTracking()
            .Where(x => x.EventType == "unanswered_question");

        if (Enum.TryParse<ProductKey>(product, true, out var parsedProduct))
        {
            query = query.Where(x => x.Product == parsedProduct);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                (x.MetadataJson != null && x.MetadataJson.Contains(term)) ||
                (x.SessionId != null && x.SessionId.Contains(term)) ||
                (x.Intent != null && x.Intent.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Product,
                x.Intent,
                x.SessionId,
                x.MetadataJson,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<UnansweredQuestionDto>(
            items.Select(x =>
            {
                var metadata = ParseMetadata(x.MetadataJson);
                return new UnansweredQuestionDto(
                    x.Id,
                    x.Product,
                    x.Intent,
                    x.SessionId,
                    metadata.Question,
                    metadata.Language,
                    metadata.Reason,
                    x.CreatedAt);
            }).ToList(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<PagedResult<AnswerFeedbackDto>> GetAnswerFeedbackAsync(int page, int pageSize, string? product, string? search, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var query = dbContext.ChatAnalyticsEvents
            .AsNoTracking()
            .Where(x => x.EventType == "answer_feedback");

        if (Enum.TryParse<ProductKey>(product, true, out var parsedProduct))
        {
            query = query.Where(x => x.Product == parsedProduct);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                (x.MetadataJson != null && x.MetadataJson.Contains(term)) ||
                (x.SessionId != null && x.SessionId.Contains(term)) ||
                (x.Intent != null && x.Intent.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Product,
                x.Intent,
                x.SessionId,
                x.MetadataJson,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AnswerFeedbackDto>(
            items.Select(x =>
            {
                var metadata = ParseFeedbackMetadata(x.MetadataJson);
                return new AnswerFeedbackDto(
                    x.Id,
                    x.Product,
                    x.Intent,
                    x.SessionId,
                    metadata.Rating,
                    metadata.Question,
                    metadata.Answer,
                    metadata.Language,
                    x.CreatedAt);
            }).ToList(),
            totalCount,
            page,
            pageSize);
    }

    private static (string Question, string? Language, string? Reason) ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return ("-", null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            var root = document.RootElement;
            return (
                root.TryGetProperty("question", out var question) ? question.GetString() ?? "-" : "-",
                root.TryGetProperty("language", out var language) ? language.GetString() : null,
                root.TryGetProperty("reason", out var reason) ? reason.GetString() : null);
        }
        catch
        {
            return (metadataJson, null, "metadata_parse_failed");
        }
    }

    private static (string Rating, string Question, string Answer, string? Language) ParseFeedbackMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return ("unknown", "-", "-", null);
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            var root = document.RootElement;
            return (
                root.TryGetProperty("rating", out var rating) ? rating.GetString() ?? "unknown" : "unknown",
                root.TryGetProperty("question", out var question) ? question.GetString() ?? "-" : "-",
                root.TryGetProperty("answer", out var answer) ? answer.GetString() ?? "-" : "-",
                root.TryGetProperty("language", out var language) ? language.GetString() : null);
        }
        catch
        {
            return ("metadata_parse_failed", metadataJson, "-", null);
        }
    }
}
