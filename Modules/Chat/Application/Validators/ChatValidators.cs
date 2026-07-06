using FluentValidation;
using V3RII.Application.DTOs;

namespace V3RII.Application.Validators;

public sealed class ChatAnswerRequestDtoValidator : AbstractValidator<ChatAnswerRequestDto>
{
    public ChatAnswerRequestDtoValidator()
    {
        RuleFor(x => x.Product).IsInEnum().When(x => x.Product is not null);
        RuleFor(x => x.Question).NotEmpty().MinimumLength(3).MaximumLength(1200);
        RuleFor(x => x.Language).MaximumLength(8);
        RuleFor(x => x.SessionId).MaximumLength(120);
    }
}
