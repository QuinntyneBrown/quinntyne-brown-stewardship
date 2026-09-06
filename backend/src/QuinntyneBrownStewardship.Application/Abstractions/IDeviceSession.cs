using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Application.Access;
namespace QuinntyneBrownStewardship.Application.Abstractions;

public interface IDeviceSession
{
    Task Create(Guid participantId, CancellationToken cancellationToken);
    Task Revoke(CancellationToken cancellationToken);
}
