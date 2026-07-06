using V3RII.Domain.Enums;

namespace V3RII.Application.DTOs;

public sealed record ChatAnswerRequestDto(ProductKey? Product, string Question, string? Language, string? SessionId);
public sealed record ChatAnswerDto(string Answer, IReadOnlyList<KnowledgeArticleDto> Sources, bool UsedLlm);
