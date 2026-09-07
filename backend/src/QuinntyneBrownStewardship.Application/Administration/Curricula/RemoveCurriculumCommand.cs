using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record RemoveCurriculumCommand(Guid Id) : IRequest;
