namespace QuinntyneBrownStewardship.Domain.Learning;

public static class Progress
{
    public static bool Complete(CurriculumModule module, IReadOnlyCollection<SectionCompletion> completions, DateTimeOffset? at = null)
    {
        var sections = module.Sections.Where(x => at == null || x.CreatedAt <= at).ToList();
        return sections.Count > 0 && sections.All(s => completions.Any(c => c.SectionId == s.Id && (at == null || c.CompletedAt <= at)));
    }
    public static int? Current(IReadOnlyCollection<CurriculumModule> modules, IReadOnlyCollection<SectionCompletion> completions, DateTimeOffset? at = null)
        => modules.OrderBy(x => x.Ordinal).FirstOrDefault(x => !Complete(x, completions, at))?.Ordinal;
    public static int Percent(int completed, int total) => total == 0 ? 0 : (int)Math.Round(100.0 * completed / total, MidpointRounding.AwayFromZero);
}
