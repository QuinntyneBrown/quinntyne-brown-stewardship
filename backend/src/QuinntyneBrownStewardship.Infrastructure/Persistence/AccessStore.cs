using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
namespace QuinntyneBrownStewardship.Infrastructure.Persistence;

public sealed class AccessStore(StewardshipDbContext db) : IAccessStore
{
    public Task<Participant?> FindParticipant(string email, CancellationToken ct) => db.Participants.AsNoTracking().SingleOrDefaultAsync(x => x.NormalizedEmail == email, ct);
    public async Task<bool> AddParticipant(Participant participant, CancellationToken ct)
    {
        db.Participants.Add(participant);
        try { await db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { db.Entry(participant).State = EntityState.Detached; return false; }
    }
    public async Task<DateTimeOffset?> CoolingOffUntil(string email, string origin, DateTimeOffset now, SignInOptions options, CancellationToken ct)
    {
        var since = now - options.Window;
        var recent = db.SignInAttempts.Where(x => x.At > since && !x.Refused);
        var account = recent.Where(x => x.NormalizedEmail == email && !x.Succeeded).OrderByDescending(x => x.At).Skip(options.AccountLimit - 1).Take(1).Select(x => (DateTimeOffset?)x.At);
        var address = recent.Where(x => x.Origin == origin).OrderByDescending(x => x.At).Skip(options.OriginLimit - 1).Take(1).Select(x => (DateTimeOffset?)x.At);
        var latest = await account.Concat(address).MaxAsync(ct);
        return latest + options.Window;
    }
    public async Task RecordAttempt(SignInAttempt attempt, CancellationToken ct) { db.SignInAttempts.Add(attempt); await db.SaveChangesAsync(ct); }
    public Task<Enrollment?> FindEnrollment(Guid participantId, CancellationToken ct) => db.Enrollments.AsNoTracking().Include(x => x.Cohort).SingleOrDefaultAsync(x => x.ParticipantId == participantId && x.IsActive, ct);
}
