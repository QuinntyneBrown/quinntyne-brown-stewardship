using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class EnrollParticipantCommandValidator : AbstractValidator<EnrollParticipantCommand>
{
    public EnrollParticipantCommandValidator() { RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress(); RuleFor(x => x.CohortId).NotEmpty(); }
}
