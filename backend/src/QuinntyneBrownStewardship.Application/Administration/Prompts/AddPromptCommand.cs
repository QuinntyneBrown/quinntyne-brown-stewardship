using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

public sealed record AddPromptCommand(Guid ModuleId, string Text) : IRequest<Guid>;
