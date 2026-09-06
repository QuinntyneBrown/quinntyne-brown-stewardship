using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Access;
using QuinntyneBrownStewardship.Api.Http;
namespace QuinntyneBrownStewardship.Api.Controllers;

[ApiController]
[Route("authentication")]
[AutoValidateAntiforgeryToken]
public sealed class AuthenticationController(ISender sender) : ControllerBase
{
    [HttpGet("csrf")]
    public async Task<CsrfResponse> Csrf(CancellationToken ct) => await sender.Send(new GetCsrfQuery(), ct);

    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn(SignInCommand command, CancellationToken ct) => (await sender.Send(command, ct)).ToActionResult();

    [Authorize]
    [HttpGet("session")]
    public async Task<SessionResponse> Session(CancellationToken ct) => await sender.Send(new GetSessionQuery(), ct);

    [Authorize]
    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOut(CancellationToken ct) { await sender.Send(new SignOutCommand(), ct); return NoContent(); }
}
