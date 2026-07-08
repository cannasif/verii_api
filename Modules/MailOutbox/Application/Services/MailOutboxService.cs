using Microsoft.EntityFrameworkCore;
using V3RII.Application.Common;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Domain.Entities;
using V3RII.Infrastructure.Persistence;

namespace V3RII.Infrastructure.Services;

public sealed class MailOutboxService(V3RiiDbContext dbContext) : IMailOutboxService
{
    public async Task<PagedResult<MailOutboxItemDto>> GetAllAsync(int page, int pageSize, string? status, string? search, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var now = DateTimeOffset.UtcNow;
        var query = dbContext.MailOutboxItems.AsNoTracking().AsQueryable();

        query = status?.Trim().ToLowerInvariant() switch
        {
            "sent" => query.Where(x => x.SentAt != null),
            "failed" => query.Where(x => x.SentAt == null && x.LastError != null),
            "retry-ready" => query.Where(x => x.SentAt == null && (x.NextAttemptAt == null || x.NextAttemptAt <= now)),
            "pending" => query.Where(x => x.SentAt == null && x.LastError == null),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.To.Contains(term) ||
                x.Subject.Contains(term) ||
                (x.LastError != null && x.LastError.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.SentAt != null)
            .ThenBy(x => x.NextAttemptAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);

        return new PagedResult<MailOutboxItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<MailOutboxSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var last24Hours = now.AddHours(-24);
        var query = dbContext.MailOutboxItems.AsNoTracking();

        return new MailOutboxSummaryDto(
            await query.CountAsync(x => x.SentAt == null && x.LastError == null, cancellationToken),
            await query.CountAsync(x => x.SentAt == null && x.LastError != null, cancellationToken),
            await query.CountAsync(x => x.SentAt != null && x.SentAt >= last24Hours, cancellationToken),
            await query.CountAsync(x => x.SentAt == null && (x.NextAttemptAt == null || x.NextAttemptAt <= now), cancellationToken));
    }

    public async Task<MailOutboxItemDto> RetryAsync(long id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.MailOutboxItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Mail kuyruğu kaydı bulunamadı.");

        if (item.SentAt is not null)
        {
            return Map(item);
        }

        item.NextAttemptAt = DateTimeOffset.UtcNow;
        item.LastError = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(item);
    }

    private static MailOutboxItemDto Map(MailOutboxItem item) => new(
        item.Id,
        item.To,
        item.Subject,
        item.AttemptCount,
        item.SentAt,
        item.NextAttemptAt,
        item.LastError,
        item.CreatedAt);
}
