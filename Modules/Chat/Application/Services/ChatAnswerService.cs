using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using V3RII.Application.DTOs;
using V3RII.Application.Interfaces;
using V3RII.Domain.Entities;
using V3RII.Infrastructure.Options;
using V3RII.Infrastructure.Persistence;

namespace V3RII.Infrastructure.Services;

public sealed class ChatAnswerService(V3RiiDbContext dbContext, IHttpClientFactory httpClientFactory, IOptions<LlmOptions> llmOptions) : IChatAnswerService
{
    private readonly LlmOptions _llmOptions = llmOptions.Value;

    public async Task<ChatAnswerDto> AnswerAsync(ChatAnswerRequestDto request, CancellationToken cancellationToken = default)
    {
        var (sources, hasDirectMatch) = await RetrieveSourcesAsync(request, cancellationToken);
        if (_llmOptions.Enabled && !string.IsNullOrWhiteSpace(_llmOptions.ApiKey) && sources.Count > 0)
        {
            var llmAnswer = await TryCreateLlmAnswerAsync(request, sources, cancellationToken);
            if (!string.IsNullOrWhiteSpace(llmAnswer))
            {
                return new ChatAnswerDto(llmAnswer, sources, true, hasDirectMatch);
            }
        }

        var fallbackAnswer = BuildFallbackAnswer(request, sources);
        return new ChatAnswerDto(fallbackAnswer, sources, false, hasDirectMatch);
    }

    private async Task<(List<KnowledgeArticleDto> Sources, bool HasDirectMatch)> RetrieveSourcesAsync(ChatAnswerRequestDto request, CancellationToken cancellationToken)
    {
        var terms = Tokenize(request.Question).Take(10).ToArray();
        var chunkQuery = dbContext.KnowledgeArticleChunks.AsNoTracking().Where(x => x.IsPublished);
        if (request.Product is not null)
        {
            chunkQuery = chunkQuery.Where(x => x.Product == request.Product.Value);
        }

        var chunkCandidates = await chunkQuery
            .OrderBy(x => x.Product)
            .ThenBy(x => x.KnowledgeArticleId)
            .ThenBy(x => x.ChunkIndex)
            .Take(250)
            .ToListAsync(cancellationToken);

        var rankedChunks = chunkCandidates
            .Select(chunk => new { Chunk = chunk, Score = ScoreChunk(chunk, terms) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Chunk.TokenEstimate)
            .Take(5)
            .Select(x => MapChunkAsSource(x.Chunk))
            .ToList();

        if (rankedChunks.Count > 0)
        {
            return (rankedChunks, true);
        }

        var articleQuery = dbContext.KnowledgeArticles.AsNoTracking().Where(x => x.IsPublished);
        if (request.Product is not null)
        {
            articleQuery = articleQuery.Where(x => x.Product == request.Product.Value);
        }

        var articleCandidates = await articleQuery
            .OrderBy(x => x.Product)
            .ThenBy(x => x.Title)
            .Take(50)
            .ToListAsync(cancellationToken);

        var rankedArticles = articleCandidates
            .Select(article => new { Article = article, Score = ScoreArticle(article, terms) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Select(x => Map(x.Article))
            .ToList();

        if (rankedArticles.Count > 0)
        {
            return (rankedArticles, true);
        }

        return (articleCandidates.Take(5).Select(Map).ToList(), false);
    }

    private async Task<string?> TryCreateLlmAnswerAsync(ChatAnswerRequestDto request, IReadOnlyList<KnowledgeArticleDto> sources, CancellationToken cancellationToken)
    {
        var context = string.Join("\n\n", sources.Select(x => $"# {x.Title}\n{x.Summary}\n{x.ContentMarkdown}"));
        var prompt = $"""
        You are V3RII's product support assistant. Answer in {request.Language ?? "tr"}.
        Use only the provided knowledge base context. If unsure, say the support team should review.

        Knowledge base:
        {context}

        User question:
        {request.Question}
        """;

        var client = httpClientFactory.CreateClient();
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _llmOptions.ApiKey);
        message.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = _llmOptions.Model,
            input = prompt
        }), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return payload.RootElement.TryGetProperty("output_text", out var outputText) ? outputText.GetString() : null;
    }

    private static string BuildFallbackAnswer(ChatAnswerRequestDto request, IReadOnlyList<KnowledgeArticleDto> sources)
    {
        if (sources.Count == 0)
        {
            return request.Language == "en"
                ? "I could not find a matching knowledge base article. I can create a support ticket so the team can review it."
                : "Bilgi tabanında eşleşen kayıt bulamadım. Ekibin incelemesi için destek talebi oluşturabilirim.";
        }

        var intro = request.Language == "en"
            ? "I found the most relevant knowledge base sections for this question:"
            : "Bu soru için bilgi tabanındaki en ilgili bölümleri buldum:";

        var summaries = sources
            .Take(3)
            .Select((source, index) => $"{index + 1}. {source.Title}: {TrimForAnswer(source.ContentMarkdown)}")
            .ToArray();

        var closing = request.Language == "en"
            ? "You can review the source cards below or I can create a support ticket for the team."
            : "Aşağıdaki kaynak kartlarından detayı inceleyebilirsiniz; isterseniz ekip için destek talebi de açabilirim.";

        return string.Join("\n\n", new[] { intro }.Concat(summaries).Concat(new[] { closing }));
    }

    private static KnowledgeArticleDto Map(KnowledgeArticle article) => new(article.Id, article.Product, article.Title, article.Summary, article.ContentMarkdown, article.Tags, article.IsPublished);

    private static string TrimForAnswer(string value)
    {
        var normalized = value.Replace("\r\n", "\n").Replace("\n", " ").Trim();
        return normalized.Length <= 280 ? normalized : $"{normalized[..280].Trim()}...";
    }

    private static KnowledgeArticleDto MapChunkAsSource(KnowledgeArticleChunk chunk) => new(
        chunk.KnowledgeArticleId,
        chunk.Product,
        chunk.Title,
        $"Bilgi tabanı bölümü #{chunk.ChunkIndex + 1}",
        chunk.Content,
        chunk.Tags,
        chunk.IsPublished);

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

    private static int ScoreArticle(KnowledgeArticle article, IReadOnlyList<string> terms)
    {
        if (terms.Count == 0)
        {
            return 0;
        }

        var score = 0;
        foreach (var term in terms)
        {
            if (article.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 5;
            if (article.Tags.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 4;
            if (article.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 3;
            if (article.ContentMarkdown.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 1;
        }

        return score;
    }

    private static IEnumerable<string> Tokenize(string value) =>
        value.ToLowerInvariant()
            .Split(new[] { ' ', '\n', '\t', '.', ',', ';', ':', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 2);
}
