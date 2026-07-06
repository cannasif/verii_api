using Microsoft.EntityFrameworkCore;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Domain.Entities;
using V3RII.Domain.Enums;
using V3RII.Infrastructure.Persistence;

namespace V3RII.Infrastructure.Services;

public sealed class KnowledgeService(V3RiiDbContext dbContext) : IKnowledgeService
{
    public async Task<IReadOnlyList<KnowledgeArticleDto>> SearchAsync(ProductKey? product, string? query, CancellationToken cancellationToken = default)
    {
        var articles = dbContext.KnowledgeArticles
            .AsNoTracking()
            .Where(x => x.IsPublished || product == null);

        if (product is not null)
        {
            articles = articles.Where(x => x.Product == product.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            articles = articles.Where(x =>
                x.Title.Contains(term) ||
                x.Summary.Contains(term) ||
                x.ContentMarkdown.Contains(term) ||
                x.Tags.Contains(term));
        }

        return await articles
            .OrderBy(x => x.Product)
            .ThenBy(x => x.Title)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<KnowledgeArticleDto> UpsertAsync(long? id, UpsertKnowledgeArticleRequestDto request, CancellationToken cancellationToken = default)
    {
        KnowledgeArticle article;
        if (id is null)
        {
            article = new KnowledgeArticle();
            dbContext.KnowledgeArticles.Add(article);
        }
        else
        {
            article = await dbContext.KnowledgeArticles.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Bilgi tabanı kaydı bulunamadı.");
        }

        article.Product = request.Product;
        article.Title = request.Title.Trim();
        article.Summary = request.Summary.Trim();
        article.ContentMarkdown = request.ContentMarkdown.Trim();
        article.Tags = request.Tags.Trim();
        article.IsPublished = request.IsPublished;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(article);
    }

    private static KnowledgeArticleDto Map(KnowledgeArticle article) => new(
        article.Id,
        article.Product,
        article.Title,
        article.Summary,
        article.ContentMarkdown,
        article.Tags,
        article.IsPublished);
}
