using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed class SignOutCommandHandler(IDeviceSession sessions) : IRequestHandler<SignOutCommand>
{
    public Task Handle(SignOutCommand request, CancellationToken cancellationToken) => sessions.Revoke(cancellationToken);
}
