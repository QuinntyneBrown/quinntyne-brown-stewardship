using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Notes;

public sealed record GetNotesQuery(Guid? ModuleId = null, Guid? SessionId = null) : IRequest<NotesResponse>;
