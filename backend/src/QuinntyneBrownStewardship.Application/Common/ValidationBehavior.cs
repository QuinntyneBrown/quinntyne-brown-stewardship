using MediatR;
using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Common;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var errors = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
            errors.AddRange((await validator.ValidateAsync(request, cancellationToken)).Errors);
        if (errors.Count > 0) throw new ValidationException(errors);
        return await next();
    }
}
