using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Infrastructure.Access;

namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class CountingPasswordHasher(PasswordHasher inner) : IPasswordHasher
{
    public List<string> VerifiedHashes { get; } = [];
    public string DummyHash => inner.DummyHash;
    public string Hash(string password) => inner.Hash(password);
    public bool Verify(string password, string hash) { VerifiedHashes.Add(hash); return inner.Verify(password, hash); }
}
