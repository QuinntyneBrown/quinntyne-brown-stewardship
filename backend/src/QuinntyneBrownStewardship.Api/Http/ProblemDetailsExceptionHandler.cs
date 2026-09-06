using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace QuinntyneBrownStewardship.Api.Http;

public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem;
        if (exception is ValidationException validation)
        {
            var errors = validation.Errors.GroupBy(x => char.ToLowerInvariant(x.PropertyName[0]) + x.PropertyName[1..]).ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).ToArray());
            problem = new ValidationProblemDetails(errors) { Status = 400, Title = "Check the highlighted fields." };
        }
        else
        {
            logger.LogError(exception, "Request failed. Correlation identifier: {CorrelationId}", context.TraceIdentifier);
            problem = new() { Status = 500, Title = "Something went wrong. Please try again." };
        }
        problem.Extensions["correlationId"] = context.TraceIdentifier;
        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync((object)problem, ct);
        return true;
    }
}
