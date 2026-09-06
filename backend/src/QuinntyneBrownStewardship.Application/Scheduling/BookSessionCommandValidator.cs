using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed class BookSessionCommandValidator : AbstractValidator<BookSessionCommand>
{
    public BookSessionCommandValidator() { RuleFor(x => x.SlotId).NotEmpty(); }
}
