using FluentValidation;
using Microsoft.Extensions.Options;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

public sealed class RevisePromptCommandValidator : AbstractValidator<RevisePromptCommand>
{
    public RevisePromptCommandValidator(IOptions<CurriculumOptions> options) => RuleFor(x => x.Text).Authored("prompt", options.Value.PromptMaxLength);
}
