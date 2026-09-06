using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed class RescheduleSessionCommandHandler(BookingOperations operations) : IRequestHandler<RescheduleSessionCommand, BookingResponse>
{
    public async Task<BookingResponse> Handle(RescheduleSessionCommand request, CancellationToken ct)
    {
        return await operations.Execute(request.Id, request.SlotId, ct);
    }
}
