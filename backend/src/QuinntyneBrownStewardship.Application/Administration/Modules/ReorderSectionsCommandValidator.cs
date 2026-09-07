using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed class ReorderSectionsCommandValidator : AbstractValidator<ReorderSectionsCommand>
{
    public ReorderSectionsCommandValidator() => RuleFor(x => x.Order).NotEmpty().WithMessage("Supply the sections in their new order.");
}
