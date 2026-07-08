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

public sealed record KnowledgeChunkDto(
    long Id,
    long KnowledgeArticleId,
    ProductKey Product,
    string Title,
    string Content,
    string Tags,
    int ChunkIndex,
    int TokenEstimate,
    bool IsPublished);

public sealed record KnowledgeChunkRebuildResultDto(int ArticleCount, int ChunkCount);

public sealed record ImportKnowledgeDocumentResultDto(
    KnowledgeArticleDto Article,
    int CharacterCount,
    string FileName);
