using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
namespace QuinntyneBrownStewardship.Api.Tests;

// Fails the pass after the staged one, so a reorder that flushed its staging cannot complete and must roll back.
public sealed class FailingCurriculumStore(CurriculumStore inner) : ICurriculumStore
{
    public Task<List<Curriculum>> Curricula(CancellationToken ct) => inner.Curricula(ct);
    public Task<Dictionary<Guid, int>> CohortCounts(CancellationToken ct) => inner.CohortCounts(ct);
    public Task<Curriculum?> Curriculum(Guid id, CancellationToken ct) => inner.Curriculum(id, ct);
    public Task<Curriculum?> CurriculumByKey(string key, CancellationToken ct) => inner.CurriculumByKey(key, ct);
    public Task<CurriculumModule?> Module(Guid id, CancellationToken ct) => inner.Module(id, ct);
    public Task<ModuleSection?> Section(Guid id, CancellationToken ct) => inner.Section(id, ct);
    public Task<PreparationPrompt?> Prompt(Guid id, CancellationToken ct) => inner.Prompt(id, ct);
    public Task<List<Cohort>> CohortsFollowing(Guid curriculumId, CancellationToken ct) => inner.CohortsFollowing(curriculumId, ct);
    public Task<Dictionary<Guid, List<SectionCompletion>>> CompletionsByEnrollment(Guid curriculumId, CancellationToken ct) => inner.CompletionsByEnrollment(curriculumId, ct);
    public Task<Dictionary<Guid, int>> SectionCompletionCounts(Guid moduleId, CancellationToken ct) => inner.SectionCompletionCounts(moduleId, ct);
    public Task<int> CompletionCount(Guid sectionId, CancellationToken ct) => inner.CompletionCount(sectionId, ct);
    public Task<int> ModuleNoteCount(Guid moduleId, CancellationToken ct) => inner.ModuleNoteCount(moduleId, ct);
    public Task<Dictionary<Guid, int>> PromptAnswerCounts(Guid moduleId, CancellationToken ct) => inner.PromptAnswerCounts(moduleId, ct);
    public Task<int> PromptAnswerCount(Guid promptId, CancellationToken ct) => inner.PromptAnswerCount(promptId, ct);
    public void Add<T>(T entity) where T : class => inner.Add(entity);
    public void Remove<T>(T entity) where T : class => inner.Remove(entity);
    public Task Flush(CancellationToken ct) => inner.Flush(ct);
    public Task<T> Transaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
        => inner.Transaction<T>(async token => { var result = await operation(token); throw new InvalidOperationException("Internal connection string: Server=private-database;Password=private-value"); }, ct);
}
