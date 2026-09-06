using FluentValidation;
using Microsoft.Extensions.Options;
namespace QuinntyneBrownStewardship.Application.Notes;

public sealed class SaveNoteCommandValidator : AbstractValidator<SaveNoteCommand>
{
    public SaveNoteCommandValidator(IOptions<NoteOptions> options)
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(options.Value.MaxLength);
        RuleFor(x => x.ModuleId).Must((x, _) => (x.ModuleId != null) != (x.SessionId != null)).WithMessage("Attach the note to exactly one module or session.");
        RuleFor(x => x.PromptId).Must((x, p) => p == null || x.ModuleId != null).WithMessage("A preparation answer belongs to its module.");
        RuleFor(x => x.Revision).NotNull().When(x => x.Id != null);
    }
}
