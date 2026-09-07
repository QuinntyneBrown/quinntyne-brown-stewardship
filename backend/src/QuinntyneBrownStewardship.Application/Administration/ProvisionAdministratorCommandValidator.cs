using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class ProvisionAdministratorCommandValidator : AbstractValidator<ProvisionAdministratorCommand>
{
    public ProvisionAdministratorCommandValidator()
    {
        RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(1024);
    }
}
