using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

public sealed class ReorderPromptsCommandValidator : AbstractValidator<ReorderPromptsCommand>
{
    public ReorderPromptsCommandValidator() => RuleFor(x => x.Order).NotEmpty().WithMessage("Supply the prompts in their new order.");
}
