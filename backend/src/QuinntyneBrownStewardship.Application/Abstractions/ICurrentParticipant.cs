using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Application.Access;
namespace QuinntyneBrownStewardship.Application.Abstractions;

public interface ICurrentParticipant
{
    Guid Id { get; }
    Guid SessionId { get; }
    string EmailAddress { get; }
    bool IsAdministrator { get; }
}
