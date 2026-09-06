using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed record ProvisionMentorCommand(string EmailAddress, string Password, string DisplayName) : IRequest<bool>;
