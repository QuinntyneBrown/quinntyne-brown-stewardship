using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record ChangeCurriculumKeyCommand(Guid Id, string Key) : IRequest;
