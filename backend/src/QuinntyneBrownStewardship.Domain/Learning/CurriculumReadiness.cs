namespace QuinntyneBrownStewardship.Domain.Learning;

// Whether a programme may be published: every module must give a participant something to read.
public sealed record CurriculumReadiness(bool CanPublish, string? Reason, List<CurriculumModule> EmptyModules)
{
    public static CurriculumReadiness Of(Curriculum curriculum)
    {
        if (curriculum.Modules.Count == 0)
            return new(false, "This programme cannot be published. It has no modules, so a participant would have nothing to read.", []);
        var empty = curriculum.Modules.Where(x => x.Sections.Count == 0).OrderBy(x => x.Ordinal).ToList();
        if (empty.Count == 0) return new(true, null, empty);
        var names = string.Join(", ", empty.Select(Name));
        return new(false, $"{empty.Count} module{(empty.Count == 1 ? " carries" : "s carry")} no section: {names}. Every module needs at least one before this programme can be published.", empty);
    }
    public static string Name(CurriculumModule module) => $"{module.Ordinal:00} {module.Title}";
}
