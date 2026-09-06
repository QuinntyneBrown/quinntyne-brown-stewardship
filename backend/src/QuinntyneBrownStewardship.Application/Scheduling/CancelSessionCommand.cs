using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed record CancelSessionCommand(Guid Id) : IRequest<Unit>;
