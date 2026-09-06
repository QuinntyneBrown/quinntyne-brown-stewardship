using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed class CancelSessionCommandHandler(BookingOperations operations) : IRequestHandler<CancelSessionCommand, Unit>
{
    public async Task<Unit> Handle(CancelSessionCommand request, CancellationToken ct)
    {
        await operations.Execute(request.Id, null, ct);
        return Unit.Value;
    }
}
