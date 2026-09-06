using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Notes;

public sealed record GetNoteQuery(Guid Id) : IRequest<NoteResponse>;
