using MediatR;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed record GetSessionQuery : IRequest<SessionResponse>;
