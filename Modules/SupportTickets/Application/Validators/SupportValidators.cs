using FluentValidation;
using V3RII.Application.DTOs;

namespace V3RII.Application.Validators;

public sealed class CreateSupportTicketRequestDtoValidator : AbstractValidator<CreateSupportTicketRequestDto>
{
    public CreateSupportTicketRequestDtoValidator()
    {
        RuleFor(x => x.Product).IsInEnum();
        RuleFor(x => x.Intent).NotEmpty().MaximumLength(120);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(180);
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.CompanyName).MaximumLength(180);
        RuleFor(x => x.Details).NotEmpty().MinimumLength(10).MaximumLength(5000);
        RuleFor(x => x.TranscriptJson).MaximumLength(12000);
        RuleFor(x => x.HandoffReason).MaximumLength(500);
        RuleFor(x => x.Source).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LeadSignalsJson).MaximumLength(4000);
    }
}

public sealed class UpdateSupportTicketStatusRequestDtoValidator : AbstractValidator<UpdateSupportTicketStatusRequestDto>
{
    public UpdateSupportTicketStatusRequestDtoValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.AssignedToEmail).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.AssignedToEmail));
    }
}
