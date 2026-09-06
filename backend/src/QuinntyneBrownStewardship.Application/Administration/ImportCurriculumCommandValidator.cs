using FluentValidation;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class ImportCurriculumCommandValidator : AbstractValidator<ImportCurriculumCommand>
{
    public ImportCurriculumCommandValidator()
    {
        RuleFor(x => x.Document).NotNull();
        When(x => x.Document != null, () =>
        {
            RuleFor(x => x.Document.Key).NotEmpty().MaximumLength(100).Matches("^[a-z0-9-]+$");
            RuleFor(x => x.Document.Modules).NotNull().Must(x => x != null && x.Count == 12 && x.Select(m => m.Ordinal).Order().SequenceEqual(Enumerable.Range(1, 12))).WithMessage("Supply twelve modules numbered 1 through 12.");
            RuleFor(x => x.Document.Modules).Must(ValidContent).WithMessage("Every module needs a stable identifier, title, summary, effort estimate, practice steps, ordered sections with readings, and unique prompt identifiers.");
        });
    }
    private static bool ValidContent(List<CurriculumModule>? modules)
    {
        if (modules == null || modules.Any(m => m == null)) return false;
        var ids = new List<Guid>();
        foreach (var m in modules)
        {
            if (string.IsNullOrWhiteSpace(m.Title) || m.Title.Length > 254 || string.IsNullOrWhiteSpace(m.Summary) || string.IsNullOrWhiteSpace(m.EffortEstimate) || m.PracticeSteps == null || m.PracticeSteps.Count == 0 || m.PracticeSteps.Any(string.IsNullOrWhiteSpace) || m.Sections == null || m.Sections.Count == 0 || m.PreparationPrompts == null) return false;
            ids.Add(m.Id);
            if (!m.Sections.Select(s => s.Ordinal).Order().SequenceEqual(Enumerable.Range(1, m.Sections.Count))) return false;
            if (!m.PreparationPrompts.Select(p => p.Ordinal).Order().SequenceEqual(Enumerable.Range(1, m.PreparationPrompts.Count))) return false;
            foreach (var s in m.Sections) { if (string.IsNullOrWhiteSpace(s.Title) || string.IsNullOrWhiteSpace(s.Reading)) return false; ids.Add(s.Id); }
            foreach (var p in m.PreparationPrompts) { if (string.IsNullOrWhiteSpace(p.Text)) return false; ids.Add(p.Id); }
        }
        return !ids.Contains(Guid.Empty) && ids.Distinct().Count() == ids.Count;
    }
}
