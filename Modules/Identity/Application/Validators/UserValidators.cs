using FluentValidation;
using V3RII.Application.DTOs;

namespace V3RII.Application.Validators;

public sealed class CreateUserRequestDtoValidator : AbstractValidator<CreateUserRequestDto>
{
    public CreateUserRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(180);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(10).MaximumLength(128);
        RuleFor(x => x.PermissionCodes).NotNull();
        RuleForEach(x => x.PermissionCodes).NotEmpty().MaximumLength(120);
    }
}
