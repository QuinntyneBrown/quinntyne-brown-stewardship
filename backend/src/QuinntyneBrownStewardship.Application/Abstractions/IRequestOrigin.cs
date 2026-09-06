using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Application.Access;
namespace QuinntyneBrownStewardship.Application.Abstractions;

public interface IRequestOrigin
{
    string Address { get; }
}
