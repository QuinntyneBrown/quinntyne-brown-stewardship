using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

public sealed record ReorderPromptsCommand(Guid ModuleId, List<Guid> Order) : IRequest;
