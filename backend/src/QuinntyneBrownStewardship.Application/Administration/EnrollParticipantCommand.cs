using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed record EnrollParticipantCommand(string EmailAddress, Guid CohortId) : IRequest<Guid>;
