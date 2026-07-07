using FluentValidation;
using V3RII.Application.DTOs;

namespace V3RII.Application.Validators;

public sealed class VoiceSynthesisRequestDtoValidator : AbstractValidator<VoiceSynthesisRequestDto>
{
    public VoiceSynthesisRequestDtoValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MinimumLength(2).MaximumLength(1400);
        RuleFor(x => x.Language).MaximumLength(8);
        RuleFor(x => x.Persona)
            .Must(x => string.IsNullOrWhiteSpace(x) || x.Equals("female", StringComparison.OrdinalIgnoreCase) || x.Equals("male", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Persona female veya male olmalıdır.");
    }
}
