using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record CreateCurriculumCommand(string Key, string Title) : IRequest<Guid>;
