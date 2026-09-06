using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Learning;

public sealed record CompleteSectionCommand(Guid SectionId) : IRequest<CompletionResponse>;
