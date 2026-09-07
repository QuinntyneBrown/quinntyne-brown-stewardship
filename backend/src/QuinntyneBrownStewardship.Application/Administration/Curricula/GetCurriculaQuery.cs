using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record GetCurriculaQuery : IRequest<CurriculaResponse>;
