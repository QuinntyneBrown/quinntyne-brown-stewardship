using FluentValidation;
using Microsoft.Extensions.Options;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class ImportCurriculumCommandValidator : AbstractValidator<ImportCurriculumCommand>
{
    public ImportCurriculumCommandValidator(IOptions<CurriculumOptions> options)
    {
        var limits = options.Value;
        RuleFor(x => x.Document).NotNull();
        When(x => x.Document != null, () =>
        {
            RuleFor(x => x.Document.Key).NotEmpty().MaximumLength(limits.KeyMaxLength).Matches("^[a-z0-9-]+$");
            RuleFor(x => x.Document.Title).MaximumLength(limits.TitleMaxLength).When(x => x.Document.Title != null);
            RuleFor(x => x.Document.Modules).NotNull().Must(x => x != null && x.Count > 0 && x.Select(m => m.Ordinal).Order().SequenceEqual(Enumerable.Range(1, x.Count))).WithMessage("Supply at least one module, numbered 1 through the module count.");
            RuleFor(x => x.Document.Modules).Must(x => ValidContent(x, limits)).WithMessage("Every module needs a stable identifier, title, summary, effort estimate, practice steps, ordered sections with readings, and unique prompt identifiers, each within its stated maximum length.");
        });
    }
    private static bool ValidContent(List<CurriculumModule>? modules, CurriculumOptions limits)
    {
        if (modules == null || modules.Any(m => m == null)) return false;
        var ids = new List<Guid>();
        foreach (var m in modules)
        {
            if (Missing(m.Title, limits.TitleMaxLength) || Missing(m.Summary, limits.SummaryMaxLength) || Missing(m.EffortEstimate, limits.EffortEstimateMaxLength)) return false;
            if (m.PracticeSteps == null || m.PracticeSteps.Count == 0 || m.PracticeSteps.Any(s => Missing(s, limits.StepMaxLength)) || m.Sections == null || m.Sections.Count == 0 || m.PreparationPrompts == null) return false;
            ids.Add(m.Id);
            if (!m.Sections.Select(s => s.Ordinal).Order().SequenceEqual(Enumerable.Range(1, m.Sections.Count))) return false;
            if (!m.PreparationPrompts.Select(p => p.Ordinal).Order().SequenceEqual(Enumerable.Range(1, m.PreparationPrompts.Count))) return false;
            foreach (var s in m.Sections) { if (Missing(s.Title, limits.TitleMaxLength) || Missing(s.Reading, limits.ReadingMaxLength)) return false; ids.Add(s.Id); }
            foreach (var p in m.PreparationPrompts) { if (Missing(p.Text, limits.PromptMaxLength)) return false; ids.Add(p.Id); }
        }
        return !ids.Contains(Guid.Empty) && ids.Distinct().Count() == ids.Count;
    }
    private static bool Missing(string? value, int maximum) => string.IsNullOrWhiteSpace(value) || value.Length > maximum;
}
