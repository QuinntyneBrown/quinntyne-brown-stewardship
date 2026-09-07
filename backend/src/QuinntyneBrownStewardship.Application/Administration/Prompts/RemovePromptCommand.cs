using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

public sealed record RemovePromptCommand(Guid Id) : IRequest;
