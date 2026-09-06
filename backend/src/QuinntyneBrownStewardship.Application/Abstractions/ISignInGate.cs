using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Application.Access;
namespace QuinntyneBrownStewardship.Application.Abstractions;

public interface ISignInGate
{
    Task<T> Execute<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
