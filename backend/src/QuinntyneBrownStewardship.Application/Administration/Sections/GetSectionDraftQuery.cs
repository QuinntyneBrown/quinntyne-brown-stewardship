using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Sections;

public sealed record GetSectionDraftQuery(Guid Id) : IRequest<SectionDraftResponse>;
