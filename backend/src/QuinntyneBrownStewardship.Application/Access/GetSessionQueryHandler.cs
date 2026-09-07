using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed class GetSessionQueryHandler(ICurrentParticipant participant) : IRequestHandler<GetSessionQuery, SessionResponse>
{
    public Task<SessionResponse> Handle(GetSessionQuery request, CancellationToken cancellationToken) => Task.FromResult(new SessionResponse(participant.EmailAddress, participant.IsAdministrator));
}
