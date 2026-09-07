using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class SetAdministratorAuthorityCommandValidator : AbstractValidator<SetAdministratorAuthorityCommand>
{
    public SetAdministratorAuthorityCommandValidator()
    {
        RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress().MaximumLength(254);
    }
}
