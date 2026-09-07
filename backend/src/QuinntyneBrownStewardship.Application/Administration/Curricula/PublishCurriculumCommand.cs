using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record PublishCurriculumCommand(Guid Id) : IRequest<PublicationResponse>;
