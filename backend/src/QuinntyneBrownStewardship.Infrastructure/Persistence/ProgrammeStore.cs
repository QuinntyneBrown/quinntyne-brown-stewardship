using Microsoft.EntityFrameworkCore;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Domain.Scheduling;
using QuinntyneBrownStewardship.Domain.Notes;
namespace QuinntyneBrownStewardship.Infrastructure.Persistence;

public sealed class ProgrammeStore(StewardshipDbContext db) : IProgrammeStore
{
    public Task<Enrollment?> Enrollment(Guid participantId, CancellationToken ct) => db.Enrollments.Include(x => x.Cohort).SingleOrDefaultAsync(x => x.ParticipantId == participantId && x.IsActive, ct);
    public Task<Enrollment?> EnrollmentById(Guid id, CancellationToken ct) => db.Enrollments.Include(x => x.Cohort).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Participant?> Participant(Guid id, CancellationToken ct) => db.Participants.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Participant?> ParticipantByEmail(string email, CancellationToken ct) => db.Participants.SingleOrDefaultAsync(x => x.NormalizedEmail == email.Trim().ToUpperInvariant(), ct);
    public Task<Cohort?> Cohort(Guid id, CancellationToken ct) => db.Cohorts.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> SlotHasBookings(Guid id, CancellationToken ct) => db.Bookings.AnyAsync(x => x.SlotId == id, ct);
    public Task<List<CurriculumModule>> Modules(string key, CancellationToken ct) => db.Modules.Include(x => x.Sections).Include(x => x.PreparationPrompts).AsSplitQuery().Where(x => x.CurriculumKey == key).OrderBy(x => x.Ordinal).ToListAsync(ct);
    public Task<List<SectionCompletion>> Completions(Guid enrollmentId, CancellationToken ct) => db.Completions.Where(x => x.EnrollmentId == enrollmentId).ToListAsync(ct);
    public Task<List<AvailabilitySlot>> Slots(Guid mentorId, CancellationToken ct) => db.Availability.Where(x => x.MentorId == mentorId).OrderBy(x => x.StartsAt).ToListAsync(ct);
    public Task<List<Booking>> Bookings(Guid enrollmentId, CancellationToken ct) => db.Bookings.Include(x => x.Slot).Where(x => x.EnrollmentId == enrollmentId).ToListAsync(ct);
    public Task<List<Guid>> ClaimedSlots(Guid mentorId, CancellationToken ct) => db.Bookings.Where(x => x.CancelledAt == null && x.Slot.MentorId == mentorId).Select(x => x.SlotId).ToListAsync(ct);
    public Task<List<Note>> Notes(Guid enrollmentId, CancellationToken ct) => db.Notes.Where(x => x.EnrollmentId == enrollmentId).OrderByDescending(x => x.RevisedAt).ThenBy(x => x.Id).ToListAsync(ct);
    public Task<Note?> Note(Guid id, CancellationToken ct) => db.Notes.SingleOrDefaultAsync(x => x.Id == id, ct);
    public void Add<T>(T entity) where T : class => db.Add(entity);
    public async Task<T> Transaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // A transaction-owned database lock works across API processes and keeps
        // slot, allowance, completion and revision checks with their writes.
        await db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sp_getapplock @Resource = 'stewardship-programme-write', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Programme lock unavailable', 1;", ct);
        var result = await operation(ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }
}
