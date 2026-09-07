using FluentValidation;
using Microsoft.Extensions.Options;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed class ReviseModuleCommandValidator : AbstractValidator<ReviseModuleCommand>
{
    public ReviseModuleCommandValidator(IOptions<CurriculumOptions> options)
    {
        RuleFor(x => x.Title).Authored("title", options.Value.TitleMaxLength);
        RuleFor(x => x.Summary).Authored("summary", options.Value.SummaryMaxLength);
        RuleFor(x => x.EffortEstimate).Authored("effort estimate", options.Value.EffortEstimateMaxLength);
        RuleFor(x => x.PracticeSteps).NotNull();
        // Each step is refused by its position, so the message lands on the field it belongs to.
        RuleForEach(x => x.PracticeSteps).Authored("practice step", options.Value.StepMaxLength);
    }
}
