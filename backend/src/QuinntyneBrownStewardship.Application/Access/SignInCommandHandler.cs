using MediatR;
using Microsoft.Extensions.Options;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed class SignInCommandHandler(IAccessStore store, IPasswordHasher hasher, IDeviceSession sessions,
    ISystemClock clock, IRequestOrigin origin, IOptions<SignInOptions> options, ISignInGate gate) : IRequestHandler<SignInCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        // Serialize the check and attempt write so concurrent requests cannot bypass the limit.
        await using var lease = await gate.Enter(cancellationToken);
        var email = request.EmailAddress.Trim().ToUpperInvariant();
        var now = clock.UtcNow;
        var retryAt = await store.CoolingOffUntil(email, origin.Address, now, options.Value, cancellationToken);
        if (retryAt != null)
        {
            await store.RecordAttempt(new() { NormalizedEmail = email, Origin = origin.Address, At = now, Refused = true }, cancellationToken);
            return new(429, "Too many attempts. Try again after the cooling-off period.", retryAt);
        }
        var participant = await store.FindParticipant(email, cancellationToken);
        var verified = hasher.Verify(request.Password, participant?.PasswordHash ?? hasher.DummyHash);
        var succeeded = participant != null && verified;
        await store.RecordAttempt(new() { NormalizedEmail = email, Origin = origin.Address, At = now, Succeeded = succeeded }, cancellationToken);
        if (!succeeded) return new(401, "Email address or password is incorrect");
        await sessions.Create(participant!.Id, cancellationToken);
        return new(200);
    }
}
