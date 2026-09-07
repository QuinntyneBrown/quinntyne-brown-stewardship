using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record GetCurriculumDraftQuery(Guid Id) : IRequest<CurriculumDraftResponse>;
