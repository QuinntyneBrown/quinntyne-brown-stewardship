using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Application.Access;
namespace QuinntyneBrownStewardship.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
    string DummyHash { get; }
}
