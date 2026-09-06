using Microsoft.AspNetCore.Http;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class CorrelationContext(IHttpContextAccessor context) : ICorrelationContext
{
    public string Id => context.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
}
