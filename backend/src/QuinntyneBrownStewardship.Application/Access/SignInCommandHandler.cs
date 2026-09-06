using MediatR;
using Microsoft.Extensions.Options;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed class SignInCommandHandler(IAccessStore store, IPasswordHasher hasher, IDeviceSession sessions,
    ISystemClock clock, IRequestOrigin origin, IOptions<SignInOptions> options, ISignInGate gate) : IRequestHandler<SignInCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        var email = request.EmailAddress.Trim().ToUpperInvariant();
        // A read can rule out an existing cooldown without holding the gate.
        // Refusals are still rechecked and recorded under the gate.
        if (await store.CoolingOffUntil(email, origin.Address, clock.UtcNow, options.Value, cancellationToken) != null)
        {
            if (await gate.Execute(token => RefuseIfCoolingOff(email, token), cancellationToken) is { } refused) return refused;
        }
        var participant = await store.FindParticipant(email, cancellationToken);
        var verified = hasher.Verify(request.Password, participant?.PasswordHash ?? hasher.DummyHash);
        var succeeded = participant != null && verified;
        // Concurrent attempts may have exhausted either limit while hashing.
        // Recheck and record atomically; a refused response never reveals the result.
        return await gate.Execute(async token =>
        {
            if (await RefuseIfCoolingOff(email, token) is { } refused) return refused;
            await store.RecordAttempt(new() { NormalizedEmail = email, Origin = origin.Address, At = clock.UtcNow, Succeeded = succeeded }, token);
            if (!succeeded) return new SignInResponse(401, "Email address or password is incorrect");
            await sessions.Create(participant!.Id, token);
            return new SignInResponse(200);
        }, cancellationToken);
    }

    private async Task<SignInResponse?> RefuseIfCoolingOff(string email, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var retryAt = await store.CoolingOffUntil(email, origin.Address, now, options.Value, ct);
        if (retryAt == null) return null;
        await store.RecordAttempt(new() { NormalizedEmail = email, Origin = origin.Address, At = now, Refused = true }, ct);
        return new(429, "Too many attempts. Try again after the cooling-off period.", retryAt);
    }
}
