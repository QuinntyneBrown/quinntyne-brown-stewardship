using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Notes;

public sealed record SaveNoteCommand(string Body, Guid? Id = null, Guid? ModuleId = null, Guid? SessionId = null, Guid? PromptId = null, Guid? Revision = null) : IRequest<NoteResponse>;
