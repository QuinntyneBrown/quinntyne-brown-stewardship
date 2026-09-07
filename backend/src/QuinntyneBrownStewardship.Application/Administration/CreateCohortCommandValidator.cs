using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class CreateCohortCommandValidator : AbstractValidator<CreateCohortCommand>
{
    public CreateCohortCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.StartDate).Must(x => x > DateOnly.MinValue && x < DateOnly.MaxValue.AddDays(-104 * 7));
        RuleFor(x => x.MentorEmail).NotEmpty().EmailAddress(); RuleFor(x => x.CurriculumKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DurationWeeks).InclusiveBetween(1, 104).WithMessage("Supply a cohort duration of 1 to 104 weeks.");
        RuleFor(x => x.SessionCadenceWeeks).GreaterThan(0).WithMessage("Supply a session cadence of at least one week.")
            .LessThanOrEqualTo(x => x.DurationWeeks).WithMessage("Session cadence cannot exceed the cohort duration.");
        RuleFor(x => x.TimeZone).Must(x => !string.IsNullOrWhiteSpace(x) && TimeZoneInfo.TryFindSystemTimeZoneById(x, out _)).WithMessage("Supply a recognized time zone such as America/Toronto.");
    }
}
