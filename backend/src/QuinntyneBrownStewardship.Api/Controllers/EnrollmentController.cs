using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Enrollment;
namespace QuinntyneBrownStewardship.Api.Controllers;

[ApiController]
[Authorize]
[Route("enrollment")]
public sealed class EnrollmentController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<EnrollmentResponse> Get(CancellationToken ct) => await sender.Send(new GetEnrollmentQuery(), ct);
}
