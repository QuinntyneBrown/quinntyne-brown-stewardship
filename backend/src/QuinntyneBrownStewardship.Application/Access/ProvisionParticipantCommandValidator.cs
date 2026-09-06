using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed class ProvisionParticipantCommandValidator : AbstractValidator<ProvisionParticipantCommand>
{
    public ProvisionParticipantCommandValidator()
    {
        RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(1024);
    }
}
