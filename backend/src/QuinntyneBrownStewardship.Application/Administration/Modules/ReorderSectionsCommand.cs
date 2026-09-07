using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed record ReorderSectionsCommand(Guid ModuleId, List<Guid> Order) : IRequest;
