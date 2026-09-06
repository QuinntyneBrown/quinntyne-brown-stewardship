using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed class BookSessionCommandHandler(BookingOperations operations) : IRequestHandler<BookSessionCommand, BookingResponse>
{
    public async Task<BookingResponse> Handle(BookSessionCommand request, CancellationToken ct)
    {
        return await operations.Execute(null, request.SlotId, ct);
    }
}
