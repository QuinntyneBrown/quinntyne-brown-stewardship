using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Notes;

public sealed record GetNotesQuery(Guid? ModuleId = null, Guid? SessionId = null, string? Cursor = null, bool GeneralOnly = false) : IRequest<NotesResponse>;
