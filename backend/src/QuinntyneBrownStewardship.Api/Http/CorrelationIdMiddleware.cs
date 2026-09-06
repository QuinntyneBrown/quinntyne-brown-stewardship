namespace QuinntyneBrownStewardship.Api.Http;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
            context.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = context.TraceIdentifier });
        await next(context);
    }
}
