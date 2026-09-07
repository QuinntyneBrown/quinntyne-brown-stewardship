using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace QuinntyneBrownStewardship.Infrastructure.Persistence;

// A transaction-owned database lock works across API processes and keeps slot,
// allowance, completion, revision and ordering checks with their writes.
internal static class ProgrammeLock
{
    public static Task Acquire(DatabaseFacade database, CancellationToken ct)
        => database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sp_getapplock @Resource = 'stewardship-programme-write', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Programme lock unavailable', 1;", ct);
}
