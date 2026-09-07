using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class SetAdministratorAuthorityCommandHandler(IAccessStore store) : IRequestHandler<SetAdministratorAuthorityCommand, bool>
{
    public Task<bool> Handle(SetAdministratorAuthorityCommand request, CancellationToken cancellationToken)
        => store.SetAdministrator(request.EmailAddress.Trim().ToUpperInvariant(), request.IsAdministrator, cancellationToken);
}
