using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed record ProvisionAdministratorCommand(string EmailAddress, string Password) : IRequest<bool>;
