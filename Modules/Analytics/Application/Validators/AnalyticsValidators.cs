using FluentValidation;
using V3RII.Application.DTOs;

namespace V3RII.Application.Validators;

public sealed class TrackChatEventRequestDtoValidator : AbstractValidator<TrackChatEventRequestDto>
{
    public TrackChatEventRequestDtoValidator()
    {
        RuleFor(x => x.Product).IsInEnum().When(x => x.Product is not null);
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Intent).MaximumLength(120);
        RuleFor(x => x.SessionId).MaximumLength(120);
        RuleFor(x => x.MetadataJson).MaximumLength(4000);
    }
}
