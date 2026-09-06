using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed class ProvisionParticipantCommandHandler(IAccessStore store, IPasswordHasher hasher) : IRequestHandler<ProvisionParticipantCommand, bool>
{
    public Task<bool> Handle(ProvisionParticipantCommand request, CancellationToken cancellationToken) => store.AddParticipant(new()
    {
        EmailAddress = request.EmailAddress.Trim(),
        NormalizedEmail = request.EmailAddress.Trim().ToUpperInvariant(),
        PasswordHash = hasher.Hash(request.Password)
    }, cancellationToken);
}
