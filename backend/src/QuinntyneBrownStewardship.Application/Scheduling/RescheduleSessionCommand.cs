using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed record RescheduleSessionCommand(Guid Id, Guid SlotId) : IRequest<BookingResponse>;
