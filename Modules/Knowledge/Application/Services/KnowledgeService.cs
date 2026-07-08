using Microsoft.EntityFrameworkCore;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Domain.Entities;
using V3RII.Domain.Enums;
using V3RII.Infrastructure.Persistence;

namespace V3RII.Infrastructure.Services;

public sealed class KnowledgeService(V3RiiDbContext dbContext) : IKnowledgeService
{
    private const int TargetChunkCharacters = 1200;
    private const int OverlapCharacters = 180;

    public async Task<IReadOnlyList<KnowledgeArticleDto>> SearchAsync(ProductKey? product, string? query, bool includeUnpublished = false, CancellationToken cancellationToken = default)
    {
        var articles = dbContext.KnowledgeArticles
            .AsNoTracking()
            .Where(x => includeUnpublished || x.IsPublished);

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
        await RebuildArticleChunksAsync(article, cancellationToken);
        return Map(article);
    }

    public async Task<ImportKnowledgeDocumentResultDto> ImportDocumentAsync(ProductKey product, string title, string content, string tags, string fileName, bool isPublished, CancellationToken cancellationToken = default)
    {
        var normalizedContent = NormalizeImportedContent(content);
        if (string.IsNullOrWhiteSpace(normalizedContent))
        {
            throw new InvalidOperationException("İçe aktarılacak doküman içeriği boş.");
        }

        var cleanTitle = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(fileName)
            : title.Trim();
        cleanTitle = Truncate(cleanTitle, 220);
        var cleanTags = string.Join(',', new[] { tags, "document-import", Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant() }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        cleanTags = Truncate(cleanTags, 700);

        var article = await UpsertAsync(null, new UpsertKnowledgeArticleRequestDto(
            product,
            cleanTitle,
            CreateSummary(normalizedContent),
            normalizedContent,
            cleanTags,
            isPublished), cancellationToken);

        return new ImportKnowledgeDocumentResultDto(article, normalizedContent.Length, fileName);
    }

    public async Task<IReadOnlyList<KnowledgeChunkDto>> SearchChunksAsync(ProductKey? product, string? query, bool includeUnpublished = false, int take = 20, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 50);
        var chunks = dbContext.KnowledgeArticleChunks
            .AsNoTracking()
            .Where(x => includeUnpublished || x.IsPublished);

        if (product is not null)
        {
            chunks = chunks.Where(x => x.Product == product.Value);
        }

        var terms = Tokenize(query ?? string.Empty).Take(8).ToArray();
        var candidates = await chunks
            .OrderBy(x => x.Product)
            .ThenBy(x => x.KnowledgeArticleId)
            .ThenBy(x => x.ChunkIndex)
            .Take(500)
            .ToListAsync(cancellationToken);

        return candidates
            .Select(chunk => new { Chunk = chunk, Score = ScoreChunk(chunk, terms) })
            .Where(x => terms.Length == 0 || x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Chunk.Product)
            .ThenBy(x => x.Chunk.KnowledgeArticleId)
            .ThenBy(x => x.Chunk.ChunkIndex)
            .Take(take)
            .Select(x => MapChunk(x.Chunk))
            .ToList();
    }

    public async Task<KnowledgeChunkRebuildResultDto> RebuildChunksAsync(CancellationToken cancellationToken = default)
    {
        var articles = await dbContext.KnowledgeArticles
            .OrderBy(x => x.Product)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        dbContext.KnowledgeArticleChunks.RemoveRange(dbContext.KnowledgeArticleChunks);

        var chunkCount = 0;
        foreach (var article in articles)
        {
            chunkCount += AddArticleChunks(article);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new KnowledgeChunkRebuildResultDto(articles.Count, chunkCount);
    }

    private async Task RebuildArticleChunksAsync(KnowledgeArticle article, CancellationToken cancellationToken)
    {
        var existing = await dbContext.KnowledgeArticleChunks
            .Where(x => x.KnowledgeArticleId == article.Id)
            .ToListAsync(cancellationToken);
        dbContext.KnowledgeArticleChunks.RemoveRange(existing);
        AddArticleChunks(article);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private int AddArticleChunks(KnowledgeArticle article)
    {
        var source = string.Join("\n\n", new[] { article.Summary, article.ContentMarkdown }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var chunks = SplitIntoChunks(source);
        for (var index = 0; index < chunks.Count; index++)
        {
            dbContext.KnowledgeArticleChunks.Add(new KnowledgeArticleChunk
            {
                KnowledgeArticleId = article.Id,
                Product = article.Product,
                Title = article.Title,
                Content = chunks[index],
                Tags = article.Tags,
                ChunkIndex = index,
                TokenEstimate = EstimateTokens(chunks[index]),
                IsPublished = article.IsPublished
            });
        }

        return chunks.Count;
    }

    private static IReadOnlyList<string> SplitIntoChunks(string value)
    {
        var normalized = value.Replace("\r\n", "\n").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var paragraphs = normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        var chunks = new List<string>();
        var current = string.Empty;

        foreach (var paragraph in paragraphs)
        {
            var candidate = string.IsNullOrWhiteSpace(current) ? paragraph : $"{current}\n{paragraph}";
            if (candidate.Length <= TargetChunkCharacters)
            {
                current = candidate;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(current))
            {
                chunks.Add(current.Trim());
                current = CreateOverlap(current);
            }

            if (paragraph.Length <= TargetChunkCharacters)
            {
                current = string.IsNullOrWhiteSpace(current) ? paragraph : $"{current}\n{paragraph}";
                continue;
            }

            for (var index = 0; index < paragraph.Length; index += TargetChunkCharacters - OverlapCharacters)
            {
                chunks.Add(paragraph.Substring(index, Math.Min(TargetChunkCharacters, paragraph.Length - index)).Trim());
            }
            current = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(current))
        {
            chunks.Add(current.Trim());
        }

        return chunks.Distinct().ToArray();
    }

    private static string CreateOverlap(string value)
    {
        if (value.Length <= OverlapCharacters)
        {
            return value;
        }

        var overlap = value[^OverlapCharacters..];
        var firstWhitespace = overlap.IndexOf(' ');
        return firstWhitespace > 0 ? overlap[firstWhitespace..].Trim() : overlap.Trim();
    }

    private static IEnumerable<string> Tokenize(string value) =>
        value.ToLowerInvariant()
            .Split(new[] { ' ', '\n', '\t', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 2);

    private static int EstimateTokens(string value) => Math.Max(1, (int)Math.Ceiling(value.Length / 4m));

    private static string NormalizeImportedContent(string value) =>
        value
            .Replace("\u0000", string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Trim();

    private static string CreateSummary(string value)
    {
        var normalized = value.Replace("\n", " ").Trim();
        return normalized.Length <= 450 ? normalized : $"{normalized[..450].Trim()}...";
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength].Trim();

    private static int ScoreChunk(KnowledgeArticleChunk chunk, IReadOnlyList<string> terms)
    {
        if (terms.Count == 0)
        {
            return 0;
        }

        var score = 0;
        foreach (var term in terms)
        {
            if (chunk.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 5;
            if (chunk.Tags.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 4;
            if (chunk.Content.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 2;
        }

        return score;
    }

    private static KnowledgeArticleDto Map(KnowledgeArticle article) => new(
        article.Id,
        article.Product,
        article.Title,
        article.Summary,
        article.ContentMarkdown,
        article.Tags,
        article.IsPublished);

    private static KnowledgeChunkDto MapChunk(KnowledgeArticleChunk chunk) => new(
        chunk.Id,
        chunk.KnowledgeArticleId,
        chunk.Product,
        chunk.Title,
        chunk.Content,
        chunk.Tags,
        chunk.ChunkIndex,
        chunk.TokenEstimate,
        chunk.IsPublished);
}
