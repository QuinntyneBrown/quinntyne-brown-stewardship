using FluentValidation;
using Microsoft.Extensions.Options;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class CreateCurriculumCommandValidator : AbstractValidator<CreateCurriculumCommand>
{
    public CreateCurriculumCommandValidator(IOptions<CurriculumOptions> options)
    {
        RuleFor(x => x.Key).Key(options.Value.KeyMaxLength);
        RuleFor(x => x.Title).Authored("title", options.Value.TitleMaxLength);
    }
}
