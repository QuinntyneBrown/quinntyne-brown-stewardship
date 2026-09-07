using FluentValidation;
using Microsoft.Extensions.Options;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class RenameCurriculumCommandValidator : AbstractValidator<RenameCurriculumCommand>
{
    public RenameCurriculumCommandValidator(IOptions<CurriculumOptions> options) => RuleFor(x => x.Title).Authored("title", options.Value.TitleMaxLength);
}
