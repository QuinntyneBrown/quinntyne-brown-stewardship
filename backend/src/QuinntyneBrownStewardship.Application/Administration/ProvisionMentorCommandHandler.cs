using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class ProvisionMentorCommandHandler(IProgrammeStore store, IPasswordHasher hasher) : IRequestHandler<ProvisionMentorCommand, bool>
{
    public async Task<bool> Handle(ProvisionMentorCommand request, CancellationToken ct)
    {
        var hash = hasher.Hash(request.Password);
        return await store.Transaction(async token =>
        {
            if (await store.ParticipantByEmail(request.EmailAddress, token) != null) return false;
            store.Add(new QuinntyneBrownStewardship.Domain.Access.Participant { EmailAddress = request.EmailAddress.Trim(), NormalizedEmail = request.EmailAddress.Trim().ToUpperInvariant(), PasswordHash = hash, IsMentor = true, DisplayName = request.DisplayName.Trim() });
            return true;
        }, ct);
    }
}
