using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed record GetPreparationQuery(Guid Id) : IRequest<PreparationResponse>;
