using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed record AddModuleCommand(Guid CurriculumId, string Title, string Summary) : IRequest<Guid>;
