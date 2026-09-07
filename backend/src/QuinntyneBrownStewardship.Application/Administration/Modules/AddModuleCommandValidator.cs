using FluentValidation;
using Microsoft.Extensions.Options;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed class AddModuleCommandValidator : AbstractValidator<AddModuleCommand>
{
    public AddModuleCommandValidator(IOptions<CurriculumOptions> options)
    {
        RuleFor(x => x.Title).Authored("title", options.Value.TitleMaxLength);
        RuleFor(x => x.Summary).Authored("summary", options.Value.SummaryMaxLength);
    }
}
