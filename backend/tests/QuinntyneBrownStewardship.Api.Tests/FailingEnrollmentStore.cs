using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Infrastructure.Persistence;

namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class FailingEnrollmentStore(AccessStore inner) : IAccessStore
{
    public Task<Participant?> FindParticipant(string email, CancellationToken ct) => inner.FindParticipant(email, ct);
    public Task<bool> AddParticipant(Participant participant, CancellationToken ct) => inner.AddParticipant(participant, ct);
    public Task<DateTimeOffset?> CoolingOffUntil(string email, string origin, DateTimeOffset now, SignInOptions options, CancellationToken ct) => inner.CoolingOffUntil(email, origin, now, options, ct);
    public Task RecordAttempt(SignInAttempt attempt, CancellationToken ct) => inner.RecordAttempt(attempt, ct);
    public Task<Enrollment?> FindEnrollment(Guid participantId, CancellationToken ct) => throw new InvalidOperationException("Internal connection string: Server=private-database;Password=private-value");
}
