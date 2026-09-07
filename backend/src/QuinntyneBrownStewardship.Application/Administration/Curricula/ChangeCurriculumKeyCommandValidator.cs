using FluentValidation;
using Microsoft.Extensions.Options;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class ChangeCurriculumKeyCommandValidator : AbstractValidator<ChangeCurriculumKeyCommand>
{
    public ChangeCurriculumKeyCommandValidator(IOptions<CurriculumOptions> options) => RuleFor(x => x.Key).Key(options.Value.KeyMaxLength);
}
