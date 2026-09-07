using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record ReorderModulesCommand(Guid CurriculumId, List<Guid> Order) : IRequest;
