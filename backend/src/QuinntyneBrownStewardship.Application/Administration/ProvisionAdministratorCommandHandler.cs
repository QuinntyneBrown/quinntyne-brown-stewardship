using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Administration;

// The first administrator cannot be made on a screen that requires one, so an operator makes it here.
public sealed class ProvisionAdministratorCommandHandler(IAccessStore store, IPasswordHasher hasher) : IRequestHandler<ProvisionAdministratorCommand, bool>
{
    public Task<bool> Handle(ProvisionAdministratorCommand request, CancellationToken cancellationToken) => store.AddParticipant(new()
    {
        EmailAddress = request.EmailAddress.Trim(),
        NormalizedEmail = request.EmailAddress.Trim().ToUpperInvariant(),
        PasswordHash = hasher.Hash(request.Password),
        IsAdministrator = true
    }, cancellationToken);
}
