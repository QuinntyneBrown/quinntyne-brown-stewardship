using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Application.Administration.Sections;
using QuinntyneBrownStewardship.Infrastructure.Access;
namespace QuinntyneBrownStewardship.Api.Controllers.Administration;

[ApiController, Authorize(Policy = AdministrationPolicy.Name), AutoValidateAntiforgeryToken, Route("administration/sections")]
public sealed class SectionsController(ISender sender) : ControllerBase
{
    [HttpGet("{id}")]
    public Task<SectionDraftResponse> Get(Guid id, CancellationToken ct) => sender.Send(new GetSectionDraftQuery(id), ct);
    [HttpPut("{id}")]
    public Task<RevisionResponse> Revise(Guid id, ReviseSectionCommand command, CancellationToken ct) => sender.Send(command with { Id = id }, ct);
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct) { await sender.Send(new RemoveSectionCommand(id), ct); return NoContent(); }
}
