using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class CreateCohortCommandValidator : AbstractValidator<CreateCohortCommand>
{
    public CreateCohortCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.StartDate).Must(x => x > DateOnly.MinValue && x < DateOnly.MaxValue.AddDays(-84));
        RuleFor(x => x.MentorEmail).NotEmpty().EmailAddress(); RuleFor(x => x.CurriculumKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TimeZone).Must(x => !string.IsNullOrWhiteSpace(x) && TimeZoneInfo.TryFindSystemTimeZoneById(x, out _)).WithMessage("Supply a recognized time zone such as America/Toronto.");
    }
}
