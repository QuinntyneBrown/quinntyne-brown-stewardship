using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed class GetCsrfQueryHandler(ICsrfToken token) : IRequestHandler<GetCsrfQuery, CsrfResponse>
{
    public Task<CsrfResponse> Handle(GetCsrfQuery request, CancellationToken cancellationToken) => Task.FromResult(new CsrfResponse(token.Issue()));
}
