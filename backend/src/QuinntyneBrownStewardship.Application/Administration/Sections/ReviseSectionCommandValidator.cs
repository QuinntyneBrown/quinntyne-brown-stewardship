using FluentValidation;
using Microsoft.Extensions.Options;
namespace QuinntyneBrownStewardship.Application.Administration.Sections;

public sealed class ReviseSectionCommandValidator : AbstractValidator<ReviseSectionCommand>
{
    public ReviseSectionCommandValidator(IOptions<CurriculumOptions> options)
    {
        RuleFor(x => x.Title).Authored("title", options.Value.TitleMaxLength);
        RuleFor(x => x.Reading).Authored("reading", options.Value.ReadingMaxLength);
    }
}
