using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Infrastructure.Persistence;

public sealed class CurriculumStore(StewardshipDbContext db) : ICurriculumStore
{
    // The index needs module identities and states, not their reading material.
    public Task<List<Curriculum>> Curricula(CancellationToken ct) => db.Curricula.AsNoTracking().Include(x => x.Modules).ToListAsync(ct);
    public async Task<Dictionary<Guid, int>> CohortCounts(CancellationToken ct)
        => await db.Cohorts.GroupBy(x => x.CurriculumId).Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    public Task<Curriculum?> Curriculum(Guid id, CancellationToken ct)
        => db.Curricula.Include(x => x.Modules).ThenInclude(x => x.Sections).Include(x => x.Modules).ThenInclude(x => x.PreparationPrompts).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Curriculum?> CurriculumByKey(string key, CancellationToken ct) => db.Curricula.SingleOrDefaultAsync(x => x.Key == key, ct);
    public Task<CurriculumModule?> Module(Guid id, CancellationToken ct)
        => db.Modules.Include(x => x.Sections).Include(x => x.PreparationPrompts).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<ModuleSection?> Section(Guid id, CancellationToken ct) => db.Sections.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<PreparationPrompt?> Prompt(Guid id, CancellationToken ct) => db.Prompts.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<List<Cohort>> CohortsFollowing(Guid curriculumId, CancellationToken ct) => db.Cohorts.AsNoTracking().Where(x => x.CurriculumId == curriculumId).OrderBy(x => x.StartDate).ToListAsync(ct);
    public async Task<Dictionary<Guid, List<SectionCompletion>>> CompletionsByEnrollment(Guid curriculumId, CancellationToken ct)
    {
        var enrollments = await db.Enrollments.AsNoTracking().Where(x => x.Cohort.CurriculumId == curriculumId).Select(x => x.Id).ToListAsync(ct);
        var completions = await db.Completions.AsNoTracking().Where(x => enrollments.Contains(x.EnrollmentId)).ToListAsync(ct);
        return enrollments.ToDictionary(id => id, id => completions.Where(x => x.EnrollmentId == id).ToList());
    }
    public async Task<Dictionary<Guid, int>> SectionCompletionCounts(Guid moduleId, CancellationToken ct)
        => await db.Completions.Where(c => db.Sections.Any(s => s.Id == c.SectionId && s.ModuleId == moduleId)).GroupBy(c => c.SectionId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    public Task<int> CompletionCount(Guid sectionId, CancellationToken ct) => db.Completions.CountAsync(x => x.SectionId == sectionId, ct);
    public Task<int> ModuleNoteCount(Guid moduleId, CancellationToken ct) => db.Notes.CountAsync(x => x.ModuleId == moduleId && x.PromptId == null, ct);
    public async Task<Dictionary<Guid, int>> PromptAnswerCounts(Guid moduleId, CancellationToken ct)
        => await db.Notes.Where(n => n.PromptId != null && db.Prompts.Any(p => p.Id == n.PromptId && p.ModuleId == moduleId)).GroupBy(n => n.PromptId!.Value).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    public Task<int> PromptAnswerCount(Guid promptId, CancellationToken ct) => db.Notes.CountAsync(x => x.PromptId == promptId, ct);
    public void Add<T>(T entity) where T : class => db.Add(entity);
    public void Remove<T>(T entity) where T : class => db.Remove(entity);
    public Task Flush(CancellationToken ct) => Save(ct);
    public async Task<T> Transaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await ProgrammeLock.Acquire(db.Database, ct);
        var result = await operation(ct);
        await Save(ct);
        await transaction.CommitAsync(ct);
        return result;
    }
    // A stale revision or a taken key is a refusal the author can act on; anything else stays a failure with a correlation id.
    private async Task Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ProgrammeException(409, "This content changed since it was opened. Reload it to see the current content."); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 } sql && sql.Message.Contains("IX_Curricula_Key"))
        { throw new ProgrammeException(409, "The key is already used by another programme. Choose a different key."); }
    }
}
