using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Administration;
using QuinntyneBrownStewardship.Application.Administration.Curricula;
using QuinntyneBrownStewardship.Application.Administration.Modules;
using QuinntyneBrownStewardship.Application.Administration.Prompts;
using QuinntyneBrownStewardship.Application.Administration.Sections;
using QuinntyneBrownStewardship.Infrastructure.Access;
namespace QuinntyneBrownStewardship.Api.Controllers.Administration;

[ApiController, Authorize(Policy = AdministrationPolicy.Name), AutoValidateAntiforgeryToken, Route("administration/modules")]
public sealed class ModulesController(ISender sender) : ControllerBase
{
    [HttpGet("{id}")]
    public Task<ModuleDraftResponse> Get(Guid id, CancellationToken ct) => sender.Send(new GetModuleDraftQuery(id), ct);
    [HttpPut("{id}")]
    public Task<RevisionResponse> Revise(Guid id, ReviseModuleCommand command, CancellationToken ct) => sender.Send(command with { Id = id }, ct);
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct) { await sender.Send(new RemoveModuleCommand(id), ct); return NoContent(); }
    [HttpPut("{id}/sections/order")]
    public async Task<IActionResult> ReorderSections(Guid id, OrderRequest request, CancellationToken ct) { await sender.Send(new ReorderSectionsCommand(id, request.Order), ct); return NoContent(); }
    [HttpPut("{id}/prompts/order")]
    public async Task<IActionResult> ReorderPrompts(Guid id, OrderRequest request, CancellationToken ct) { await sender.Send(new ReorderPromptsCommand(id, request.Order), ct); return NoContent(); }
    [HttpPost("{id}/sections")]
    public async Task<CreatedResponse> AddSection(Guid id, AddSectionCommand command, CancellationToken ct) => new(await sender.Send(command with { ModuleId = id }, ct));
    [HttpPost("{id}/prompts")]
    public async Task<CreatedResponse> AddPrompt(Guid id, AddPromptCommand command, CancellationToken ct) => new(await sender.Send(command with { ModuleId = id }, ct));
}
