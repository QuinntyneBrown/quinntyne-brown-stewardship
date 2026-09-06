using Microsoft.AspNetCore.Http;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class RequestOrigin(IHttpContextAccessor context) : IRequestOrigin
{
    public string Address => context.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "local";
}
