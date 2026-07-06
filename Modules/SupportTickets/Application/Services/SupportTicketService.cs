using Microsoft.EntityFrameworkCore;
using V3RII.Application.Common;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Domain.Entities;
using V3RII.Domain.Enums;
using V3RII.Infrastructure.Persistence;

namespace V3RII.Infrastructure.Services;

public sealed class SupportTicketService(V3RiiDbContext dbContext) : ISupportTicketService
{
    public async Task<SupportTicketDto> CreateAsync(CreateSupportTicketRequestDto request, CancellationToken cancellationToken = default)
    {
        var ticket = new SupportTicket
        {
            TicketNo = await CreateTicketNumberAsync(cancellationToken),
            Product = request.Product,
            Intent = request.Intent.Trim(),
            Priority = ResolvePriority(request.Intent, request.Details),
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? null : request.CompanyName.Trim(),
            Details = request.Details.Trim(),
            TranscriptJson = request.TranscriptJson,
            RequiresHandoff = request.RequiresHandoff,
            HandoffReason = string.IsNullOrWhiteSpace(request.HandoffReason) ? null : request.HandoffReason.Trim(),
            Source = string.IsNullOrWhiteSpace(request.Source) ? "website-chatbot" : request.Source.Trim()
        };

        dbContext.SupportTickets.Add(ticket);
        dbContext.MailOutboxItems.Add(new MailOutboxItem
        {
            To = "destek@v3rii.com",
            Subject = $"Yeni destek talebi: {ticket.TicketNo}",
            BodyHtml = $"<strong>{ticket.Product}</strong><br>{ticket.CustomerName} - {ticket.CustomerEmail}<br>{ticket.Details}",
            NextAttemptAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(ticket);
    }

    public async Task<PagedResult<SupportTicketDto>> GetAllAsync(int page, int pageSize, string? status, string? product, string? search, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.SupportTickets.AsNoTracking().AsQueryable();

        if (Enum.TryParse<SupportTicketStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (Enum.TryParse<ProductKey>(product, true, out var parsedProduct))
        {
            query = query.Where(x => x.Product == parsedProduct);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.TicketNo.Contains(term) ||
                x.CustomerName.Contains(term) ||
                x.CustomerEmail.Contains(term) ||
                (x.CompanyName != null && x.CompanyName.Contains(term)) ||
                x.Details.Contains(term));
        }

        query = query.OrderByDescending(x => x.CreatedAt);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);

        return new PagedResult<SupportTicketDto>(items, totalCount, page, pageSize);
    }

    public async Task<SupportDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);
        var openStatuses = new[] { SupportTicketStatus.New, SupportTicketStatus.InProgress, SupportTicketStatus.WaitingCustomer };
        var latestTickets = await dbContext.SupportTickets
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);

        return new SupportDashboardDto(
            await dbContext.SupportTickets.CountAsync(x => openStatuses.Contains(x.Status), cancellationToken),
            await dbContext.SupportTickets.CountAsync(x => x.Priority == SupportTicketPriority.Urgent || x.Priority == SupportTicketPriority.High, cancellationToken),
            await dbContext.SupportTickets.CountAsync(x => x.RequiresHandoff && openStatuses.Contains(x.Status), cancellationToken),
            await dbContext.SupportTickets.CountAsync(x => x.Status == SupportTicketStatus.WaitingCustomer, cancellationToken),
            await dbContext.SupportTickets.CountAsync(x => x.ResolvedAt != null && x.ResolvedAt >= sevenDaysAgo, cancellationToken),
            latestTickets);
    }

    public async Task<SupportTicketDto> UpdateStatusAsync(long id, UpdateSupportTicketStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var ticket = await dbContext.SupportTickets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Destek talebi bulunamadı.");

        ticket.Status = request.Status;
        ticket.AssignedToEmail = string.IsNullOrWhiteSpace(request.AssignedToEmail) ? ticket.AssignedToEmail : request.AssignedToEmail.Trim();
        if (request.Status is SupportTicketStatus.Resolved or SupportTicketStatus.Closed)
        {
            ticket.ResolvedAt ??= DateTimeOffset.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(ticket);
    }

    private async Task<string> CreateTicketNumberAsync(CancellationToken cancellationToken)
    {
        var today = DateTimeOffset.UtcNow;
        var prefix = $"VR-{today:yyyyMMdd}";
        var count = await dbContext.SupportTickets.CountAsync(x => x.TicketNo.StartsWith(prefix), cancellationToken) + 1;
        return $"{prefix}-{count:0000}";
    }

    private static SupportTicketPriority ResolvePriority(string intent, string details)
    {
        var text = $"{intent} {details}".ToLowerInvariant();
        if (text.Contains("hata") || text.Contains("error") || text.Contains("acil") || text.Contains("çalışmıyor"))
        {
            return SupportTicketPriority.High;
        }

        return SupportTicketPriority.Normal;
    }

    private static SupportTicketDto Map(SupportTicket ticket) => new(
        ticket.Id,
        ticket.TicketNo,
        ticket.Product,
        ticket.Intent,
        ticket.Status,
        ticket.Priority,
        ticket.CustomerName,
        ticket.CustomerEmail,
        ticket.CompanyName,
        ticket.Details,
        ticket.RequiresHandoff,
        ticket.HandoffReason,
        ticket.Source,
        ticket.AssignedToEmail,
        ticket.CreatedAt,
        ticket.ResolvedAt);
}
