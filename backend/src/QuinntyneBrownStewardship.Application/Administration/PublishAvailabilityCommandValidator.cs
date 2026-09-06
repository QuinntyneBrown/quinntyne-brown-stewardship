using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class PublishAvailabilityCommandValidator : AbstractValidator<PublishAvailabilityCommand>
{
    public PublishAvailabilityCommandValidator()
    {
        RuleFor(x => x.Document).NotNull();
        When(x => x.Document != null, () =>
        {
            RuleFor(x => x.Document.MentorEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.Document.Slots).NotNull().Must(x => x != null && x.Count > 0 && x.Select(s => s.Id).Distinct().Count() == x.Count && x.All(s => s.Id != Guid.Empty && s.DurationMinutes is >= 15 and <= 180 && s.StartsAt.Year < 9999)).WithMessage("Supply unique slot identifiers and durations of 15 to 180 minutes.");
        });
    }
}
