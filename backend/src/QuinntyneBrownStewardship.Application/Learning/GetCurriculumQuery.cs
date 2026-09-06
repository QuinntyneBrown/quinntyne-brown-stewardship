using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Learning;

public sealed record GetCurriculumQuery() : IRequest<CurriculumResponse>;
