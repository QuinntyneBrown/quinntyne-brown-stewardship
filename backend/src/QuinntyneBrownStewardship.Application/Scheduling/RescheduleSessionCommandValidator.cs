using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed class RescheduleSessionCommandValidator : AbstractValidator<RescheduleSessionCommand>
{
    public RescheduleSessionCommandValidator() { RuleFor(x => x.SlotId).NotEmpty(); }
}
