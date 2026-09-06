using Microsoft.EntityFrameworkCore;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class SignInLease(StewardshipDbContext db) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        try { await db.Database.ExecuteSqlRawAsync("EXEC sp_releaseapplock @Resource = 'Stewardship.SignIn', @LockOwner = 'Session'"); }
        finally { await db.Database.CloseConnectionAsync(); }
    }
}
