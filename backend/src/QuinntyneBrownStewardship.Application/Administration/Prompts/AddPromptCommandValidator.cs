using FluentValidation;
using Microsoft.Extensions.Options;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

public sealed class AddPromptCommandValidator : AbstractValidator<AddPromptCommand>
{
    public AddPromptCommandValidator(IOptions<CurriculumOptions> options) => RuleFor(x => x.Text).Authored("prompt", options.Value.PromptMaxLength);
}
