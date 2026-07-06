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
        var sources = await RetrieveSourcesAsync(request, cancellationToken);
        if (_llmOptions.Enabled && !string.IsNullOrWhiteSpace(_llmOptions.ApiKey) && sources.Count > 0)
        {
            var llmAnswer = await TryCreateLlmAnswerAsync(request, sources, cancellationToken);
            if (!string.IsNullOrWhiteSpace(llmAnswer))
            {
                return new ChatAnswerDto(llmAnswer, sources.Select(Map).ToList(), true);
            }
        }

        var fallbackAnswer = BuildFallbackAnswer(request, sources);
        return new ChatAnswerDto(fallbackAnswer, sources.Select(Map).ToList(), false);
    }

    private async Task<List<KnowledgeArticle>> RetrieveSourcesAsync(ChatAnswerRequestDto request, CancellationToken cancellationToken)
    {
        var terms = request.Question
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 2)
            .Take(8)
            .ToArray();

        var query = dbContext.KnowledgeArticles.AsNoTracking().Where(x => x.IsPublished);
        if (request.Product is not null)
        {
            query = query.Where(x => x.Product == request.Product.Value);
        }

        foreach (var term in terms)
        {
            query = query.Where(x => x.Title.Contains(term) || x.Summary.Contains(term) || x.ContentMarkdown.Contains(term) || x.Tags.Contains(term));
        }

        var strictResults = await query.Take(5).ToListAsync(cancellationToken);
        if (strictResults.Count > 0)
        {
            return strictResults;
        }

        query = dbContext.KnowledgeArticles.AsNoTracking().Where(x => x.IsPublished);
        if (request.Product is not null)
        {
            query = query.Where(x => x.Product == request.Product.Value);
        }

        return await query.OrderBy(x => x.Product).ThenBy(x => x.Title).Take(5).ToListAsync(cancellationToken);
    }

    private async Task<string?> TryCreateLlmAnswerAsync(ChatAnswerRequestDto request, IReadOnlyList<KnowledgeArticle> sources, CancellationToken cancellationToken)
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

    private static string BuildFallbackAnswer(ChatAnswerRequestDto request, IReadOnlyList<KnowledgeArticle> sources)
    {
        if (sources.Count == 0)
        {
            return request.Language == "en"
                ? "I could not find a matching knowledge base article. I can create a support ticket so the team can review it."
                : "Bilgi tabanında eşleşen kayıt bulamadım. Ekibin incelemesi için destek talebi oluşturabilirim.";
        }

        return string.Join("\n\n", sources.Select(x => $"{x.Title}\n{x.Summary}\n{x.ContentMarkdown}"));
    }

    private static KnowledgeArticleDto Map(KnowledgeArticle article) => new(article.Id, article.Product, article.Title, article.Summary, article.ContentMarkdown, article.Tags, article.IsPublished);
}
