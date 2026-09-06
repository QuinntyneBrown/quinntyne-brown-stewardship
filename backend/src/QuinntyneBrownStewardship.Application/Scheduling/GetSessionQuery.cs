using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed record GetSessionQuery(Guid Id) : IRequest<BookingResponse>;
