using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Notes;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Api.Controllers;

[ApiController, Authorize, AutoValidateAntiforgeryToken, Route("notes")]
public sealed class NotesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<NotesResponse> List([FromQuery] Guid? moduleId, [FromQuery] Guid? sessionId, CancellationToken ct) => sender.Send(new GetNotesQuery(moduleId, sessionId), ct);
    [HttpGet("{id:guid}")]
    public Task<NoteResponse> Get(Guid id, CancellationToken ct) => sender.Send(new GetNoteQuery(id), ct);
    [HttpPost]
    public Task<NoteResponse> Save(SaveNoteCommand command, CancellationToken ct) => sender.Send(command, ct);
}
