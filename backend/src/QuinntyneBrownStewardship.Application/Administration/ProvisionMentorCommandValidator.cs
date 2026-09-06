using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class ProvisionMentorCommandValidator : AbstractValidator<ProvisionMentorCommand>
{
    public ProvisionMentorCommandValidator()
    {
        RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(1024);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(254);
    }
}
