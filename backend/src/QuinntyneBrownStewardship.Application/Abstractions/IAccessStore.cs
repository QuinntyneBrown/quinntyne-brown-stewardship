using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Application.Access;
namespace QuinntyneBrownStewardship.Application.Abstractions;

public interface IAccessStore
{
    Task<Participant?> FindParticipant(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> AddParticipant(Participant participant, CancellationToken cancellationToken);
    Task<DateTimeOffset?> CoolingOffUntil(string normalizedEmail, string origin, DateTimeOffset now, SignInOptions options, CancellationToken cancellationToken);
    Task RecordAttempt(SignInAttempt attempt, CancellationToken cancellationToken);
    Task<Domain.Enrollment.Enrollment?> FindEnrollment(Guid participantId, CancellationToken cancellationToken);
    Task<bool> SetAdministrator(string normalizedEmail, bool isAdministrator, CancellationToken cancellationToken);
}
