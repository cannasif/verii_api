using FluentValidation;
using V3RII.Application.DTOs;

namespace V3RII.Application.Validators;

public sealed class UpsertKnowledgeArticleRequestDtoValidator : AbstractValidator<UpsertKnowledgeArticleRequestDto>
{
    public UpsertKnowledgeArticleRequestDtoValidator()
    {
        RuleFor(x => x.Product).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(220);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(700);
        RuleFor(x => x.ContentMarkdown).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.Tags).MaximumLength(700);
    }
}
