using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration;

// One command behind both grant-administrator and revoke-administrator; false means no account holds the address.
public sealed record SetAdministratorAuthorityCommand(string EmailAddress, bool IsAdministrator) : IRequest<bool>;
