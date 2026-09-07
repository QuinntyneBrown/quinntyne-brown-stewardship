using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record RenameCurriculumCommand(Guid Id, string Title) : IRequest;
