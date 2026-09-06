using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Learning;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Api.Controllers;

[ApiController, Authorize, Route("curriculum")]
public sealed class CurriculumController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<CurriculumResponse> Get(CancellationToken ct) => sender.Send(new GetCurriculumQuery(), ct);
}
