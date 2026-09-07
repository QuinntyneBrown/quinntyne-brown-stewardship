using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public static class ModuleLookup
{
    public static async Task<CurriculumModule> RequiredModule(this ICurriculumStore store, Guid id, CancellationToken ct)
        => await store.Module(id, ct) ?? throw new ProgrammeException(404, "Module not found.");
    public static async Task<ModuleSection> RequiredSection(this ICurriculumStore store, Guid id, CancellationToken ct)
        => await store.Section(id, ct) ?? throw new ProgrammeException(404, "Section not found.");
    public static async Task<PreparationPrompt> RequiredPrompt(this ICurriculumStore store, Guid id, CancellationToken ct)
        => await store.Prompt(id, ct) ?? throw new ProgrammeException(404, "Prompt not found.");
    public static async Task<ModuleDependents> Dependents(this ICurriculumStore store, Guid moduleId, CancellationToken ct)
        => new((await store.SectionCompletionCounts(moduleId, ct)).Values.Sum(), await store.ModuleNoteCount(moduleId, ct), (await store.PromptAnswerCounts(moduleId, ct)).Values.Sum());
    public static int WordCount(string reading) => reading.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
