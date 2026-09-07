using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Administration.Prompts;
using QuinntyneBrownStewardship.Infrastructure.Access;
namespace QuinntyneBrownStewardship.Api.Controllers.Administration;

[ApiController, Authorize(Policy = AdministrationPolicy.Name), AutoValidateAntiforgeryToken, Route("administration/prompts")]
public sealed class PromptsController(ISender sender) : ControllerBase
{
    [HttpPut("{id}")]
    public async Task<IActionResult> Revise(Guid id, RevisePromptCommand command, CancellationToken ct) { await sender.Send(command with { Id = id }, ct); return NoContent(); }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct) { await sender.Send(new RemovePromptCommand(id), ct); return NoContent(); }
}
