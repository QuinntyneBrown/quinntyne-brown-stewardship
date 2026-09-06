using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Infrastructure.Access;

namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class FailingDeviceSession(DeviceSession inner) : IDeviceSession
{
    public async Task Create(Guid participantId, CancellationToken ct)
    {
        await inner.Create(participantId, ct);
        throw new InvalidOperationException("Simulated failure before sign-in finished.");
    }
    public Task Revoke(CancellationToken ct) => inner.Revoke(ct);
}
