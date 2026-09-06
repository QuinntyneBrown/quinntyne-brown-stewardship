using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using ISystemClock = QuinntyneBrownStewardship.Application.Abstractions.ISystemClock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Infrastructure.Persistence;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class SessionAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    StewardshipDbContext db, ISystemClock clock, IOptions<SessionLifetimeOptions> lifetime) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(SessionCookie.Name, out var token) || token.Length != 64) return AuthenticateResult.NoResult();
        var hash = SessionCookie.Hash(token);
        var session = await db.Sessions.SingleOrDefaultAsync(x => x.TokenHash == hash, Context.RequestAborted);
        if (session == null || session.IsExpired(clock.UtcNow, lifetime.Value.IdleTimeout))
        {
            Response.Cookies.Delete(SessionCookie.Name, SessionCookie.Options(clock.UtcNow));
            return AuthenticateResult.Fail("Session expired");
        }
        var participant = await db.Participants.AsNoTracking().SingleAsync(x => x.Id == session.ParticipantId, Context.RequestAborted);
        session.Touch(clock.UtcNow);
        await db.SaveChangesAsync(Context.RequestAborted);
        Response.Cookies.Append(SessionCookie.Name, token, SessionCookie.Options(clock.UtcNow + lifetime.Value.IdleTimeout));
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, participant.Id.ToString()), new Claim(ClaimTypes.Email, participant.EmailAddress), new Claim("session_id", session.Id.ToString()) }, Scheme.Name);
        return AuthenticateResult.Success(new(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
