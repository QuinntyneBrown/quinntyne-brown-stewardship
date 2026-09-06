using Microsoft.EntityFrameworkCore;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class SignInGate(StewardshipDbContext db) : ISignInGate
{
    public async Task<T> Execute<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sp_getapplock @Resource = 'Stewardship.SignIn', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Sign-in is busy', 1;", ct);
        var result = await operation(ct);
        await transaction.CommitAsync(ct);
        return result;
    }
}
