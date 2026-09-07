using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class ReorderModulesCommandValidator : AbstractValidator<ReorderModulesCommand>
{
    public ReorderModulesCommandValidator() => RuleFor(x => x.Order).NotEmpty().WithMessage("Supply the modules in their new order.");
}
