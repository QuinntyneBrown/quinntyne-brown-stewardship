using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public static class SessionCookie
{
    public const string Name = "__Host-Stewardship";
    public const string Scheme = "ParticipantSession";
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public static CookieOptions Options(DateTimeOffset expires) => new() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/", Expires = expires, IsEssential = true };
}
