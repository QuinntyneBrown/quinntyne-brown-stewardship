using Microsoft.EntityFrameworkCore;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class SignInGate(StewardshipDbContext db) : ISignInGate
{
    public async Task<IAsyncDisposable> Enter(CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sp_getapplock @Resource = 'Stewardship.SignIn', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Sign-in is busy', 1;", ct);
            return new SignInLease(db);
        }
        catch { await db.Database.CloseConnectionAsync(); throw; }
    }
}
