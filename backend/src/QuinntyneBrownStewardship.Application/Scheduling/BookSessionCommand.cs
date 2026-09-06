using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed record BookSessionCommand(Guid SlotId) : IRequest<BookingResponse>;
