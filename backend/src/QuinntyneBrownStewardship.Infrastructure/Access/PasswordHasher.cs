using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Access;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> hasher;
    public string DummyHash { get; }
    public PasswordHasher(IOptions<PasswordHashingOptions> options)
    {
        hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = options.Value.IterationCount, CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3 }));
        DummyHash = Hash(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
    }
    public string Hash(string password) => hasher.HashPassword(null!, password);
    public bool Verify(string password, string hash) => hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
