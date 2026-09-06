using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed record GetAvailabilityQuery(DateOnly? Day) : IRequest<AvailabilityResponse>;
