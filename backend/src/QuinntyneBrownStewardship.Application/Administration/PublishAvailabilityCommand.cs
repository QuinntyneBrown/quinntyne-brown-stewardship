using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed record PublishAvailabilityCommand(AvailabilityImport Document) : IRequest<int>;
