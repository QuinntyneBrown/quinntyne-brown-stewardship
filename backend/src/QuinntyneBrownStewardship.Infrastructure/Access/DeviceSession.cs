using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class DeviceSession(StewardshipDbContext db, IHttpContextAccessor context, ICurrentParticipant participant,
    ISystemClock clock, IOptions<SessionLifetimeOptions> options) : IDeviceSession
{
    public async Task Create(Guid participantId, CancellationToken ct)
    {
        if (context.HttpContext!.Request.Cookies.TryGetValue(SessionCookie.Name, out var previous))
        {
            var hash = SessionCookie.Hash(previous);
            var old = await db.Sessions.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
            old?.Revoke(clock.UtcNow);
        }
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var session = new ParticipantSession { ParticipantId = participantId, TokenHash = SessionCookie.Hash(token) };
        session.Touch(clock.UtcNow);
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);
        context.HttpContext.Response.Cookies.Append(SessionCookie.Name, token, SessionCookie.Options(clock.UtcNow + options.Value.IdleTimeout));
    }
    public async Task Revoke(CancellationToken ct)
    {
        var session = await db.Sessions.SingleAsync(x => x.Id == participant.SessionId, ct);
        session.Revoke(clock.UtcNow);
        await db.SaveChangesAsync(ct);
        context.HttpContext!.Response.Cookies.Delete(SessionCookie.Name, SessionCookie.Options(clock.UtcNow));
    }
}
