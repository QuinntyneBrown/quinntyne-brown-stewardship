using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

// Only the wording changes; the identifier stays, so every answer already attached stays attached.
public sealed record RevisePromptCommand(Guid Id, string Text) : IRequest;
