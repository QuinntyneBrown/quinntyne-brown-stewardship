using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Learning;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Api.Controllers;

[ApiController, Authorize, AutoValidateAntiforgeryToken]
public sealed class ModulesController(ISender sender) : ControllerBase
{
    [HttpGet("modules/current")]
    public Task<ModuleResponse> Current(CancellationToken ct) => sender.Send(new GetModuleQuery(null), ct);
    [HttpGet("modules/{ordinal}")]
    public Task<ModuleResponse> Get(int ordinal, CancellationToken ct) => sender.Send(new GetModuleQuery(ordinal), ct);
    [HttpPost("sections/{id}/completion")]
    public Task<CompletionResponse> Complete(Guid id, CancellationToken ct) => sender.Send(new CompleteSectionCommand(id), ct);
}
