using MediatR;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed record GetCsrfQuery : IRequest<CsrfResponse>;
