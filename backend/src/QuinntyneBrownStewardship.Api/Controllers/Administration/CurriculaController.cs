using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Application.Administration.Curricula;
using QuinntyneBrownStewardship.Application.Administration.Modules;
using QuinntyneBrownStewardship.Infrastructure.Access;
namespace QuinntyneBrownStewardship.Api.Controllers.Administration;

[ApiController, Authorize(Policy = AdministrationPolicy.Name), AutoValidateAntiforgeryToken, Route("administration/curricula")]
public sealed class CurriculaController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<CurriculaResponse> List(CancellationToken ct) => sender.Send(new GetCurriculaQuery(), ct);
    [HttpPost]
    public async Task<CreatedResponse> Create(CreateCurriculumCommand command, CancellationToken ct) => new(await sender.Send(command, ct));
    [HttpGet("{id}")]
    public Task<CurriculumDraftResponse> Get(Guid id, CancellationToken ct) => sender.Send(new GetCurriculumDraftQuery(id), ct);
    // The route names the programme; the body carries only the field being saved, so anything else it holds is ignored.
    [HttpPut("{id}")]
    public async Task<IActionResult> Rename(Guid id, RenameCurriculumCommand command, CancellationToken ct) { await sender.Send(command with { Id = id }, ct); return NoContent(); }
    [HttpPut("{id}/key")]
    public async Task<IActionResult> ChangeKey(Guid id, ChangeCurriculumKeyCommand command, CancellationToken ct) { await sender.Send(command with { Id = id }, ct); return NoContent(); }
    [HttpPut("{id}/modules/order")]
    public async Task<IActionResult> ReorderModules(Guid id, OrderRequest request, CancellationToken ct) { await sender.Send(new ReorderModulesCommand(id, request.Order), ct); return NoContent(); }
    [HttpPost("{id}/publication")]
    public Task<PublicationResponse> Publish(Guid id, CancellationToken ct) => sender.Send(new PublishCurriculumCommand(id), ct);
    [HttpPost("{id}/modules")]
    public async Task<CreatedResponse> AddModule(Guid id, AddModuleCommand command, CancellationToken ct) => new(await sender.Send(command with { CurriculumId = id }, ct));
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct) { await sender.Send(new RemoveCurriculumCommand(id), ct); return NoContent(); }
}
