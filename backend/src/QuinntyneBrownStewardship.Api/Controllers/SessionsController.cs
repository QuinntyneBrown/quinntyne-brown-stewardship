using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Scheduling;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Api.Controllers;

[ApiController, Authorize, AutoValidateAntiforgeryToken, Route("sessions")]
public sealed class SessionsController(ISender sender) : ControllerBase
{
    [HttpGet("availability")]
    public Task<AvailabilityResponse> Availability([FromQuery] DateOnly? day, CancellationToken ct) => sender.Send(new GetAvailabilityQuery(day), ct);
    [HttpGet("history")]
    public Task<HistoryResponse> History(CancellationToken ct) => sender.Send(new GetSessionHistoryQuery(), ct);
    [HttpGet("{id:guid}")]
    public Task<BookingResponse> Get(Guid id, CancellationToken ct) => sender.Send(new GetSessionQuery(id), ct);
    [HttpGet("{id:guid}/preparation")]
    public Task<PreparationResponse> Preparation(Guid id, CancellationToken ct) => sender.Send(new GetPreparationQuery(id), ct);
    [HttpPost]
    public Task<BookingResponse> Book(BookSessionCommand command, CancellationToken ct) => sender.Send(command, ct);
    [HttpPut("{id:guid}/slot")]
    public Task<BookingResponse> Reschedule(Guid id, BookSessionCommand command, CancellationToken ct) => sender.Send(new RescheduleSessionCommand(id, command.SlotId), ct);
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) { await sender.Send(new CancelSessionCommand(id), ct); return NoContent(); }
}
