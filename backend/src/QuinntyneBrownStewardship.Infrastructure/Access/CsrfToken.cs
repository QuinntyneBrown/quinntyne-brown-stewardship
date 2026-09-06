using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class CsrfToken(IAntiforgery antiforgery, IHttpContextAccessor context) : ICsrfToken
{
    public string Issue() => antiforgery.GetAndStoreTokens(context.HttpContext!).RequestToken!;
}
