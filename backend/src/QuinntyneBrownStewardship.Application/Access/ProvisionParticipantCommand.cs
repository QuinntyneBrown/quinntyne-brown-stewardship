using MediatR;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed record ProvisionParticipantCommand(string EmailAddress, string Password) : IRequest<bool>;
