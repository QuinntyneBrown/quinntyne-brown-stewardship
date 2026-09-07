using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Sections;

public sealed record ReviseSectionCommand(Guid Id, string Title, string Reading, Guid Revision) : IRequest<RevisionResponse>;
