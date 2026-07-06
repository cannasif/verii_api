using V3RII.Domain.Enums;

namespace V3RII.Application.DTOs;

public sealed record KnowledgeArticleDto(
    long Id,
    ProductKey Product,
    string Title,
    string Summary,
    string ContentMarkdown,
    string Tags,
    bool IsPublished);

public sealed record UpsertKnowledgeArticleRequestDto(
    ProductKey Product,
    string Title,
    string Summary,
    string ContentMarkdown,
    string Tags,
    bool IsPublished);
