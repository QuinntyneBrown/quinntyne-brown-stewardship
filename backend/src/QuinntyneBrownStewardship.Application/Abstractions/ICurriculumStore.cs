using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Abstractions;

// Reads and writes of authored curriculum. Participant handlers never need these, and authoring handlers never need enrollments or notes.
public interface ICurriculumStore
{
    Task<List<Curriculum>> Curricula(CancellationToken ct);
    Task<Dictionary<Guid, int>> CohortCounts(CancellationToken ct);
    // The whole authored programme, tracked, with every module's sections and prompts.
    Task<Curriculum?> Curriculum(Guid id, CancellationToken ct);
    Task<Curriculum?> CurriculumByKey(string key, CancellationToken ct);
    // One module, tracked, with its sections and prompts.
    Task<CurriculumModule?> Module(Guid id, CancellationToken ct);
    Task<ModuleSection?> Section(Guid id, CancellationToken ct);
    Task<PreparationPrompt?> Prompt(Guid id, CancellationToken ct);
    Task<List<Cohort>> CohortsFollowing(Guid curriculumId, CancellationToken ct);
    // The completions of every enrollment of every cohort following the programme, including enrollments with none.
    Task<Dictionary<Guid, List<SectionCompletion>>> CompletionsByEnrollment(Guid curriculumId, CancellationToken ct);
    // What participants have recorded against a module: completions per section, notes on the module, answers per prompt.
    Task<Dictionary<Guid, int>> SectionCompletionCounts(Guid moduleId, CancellationToken ct);
    Task<int> CompletionCount(Guid sectionId, CancellationToken ct);
    Task<int> ModuleNoteCount(Guid moduleId, CancellationToken ct);
    Task<Dictionary<Guid, int>> PromptAnswerCounts(Guid moduleId, CancellationToken ct);
    Task<int> PromptAnswerCount(Guid promptId, CancellationToken ct);
    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    Task Flush(CancellationToken ct);
    Task<T> Transaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);
}
