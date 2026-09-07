using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class CurrentParticipant(IHttpContextAccessor context) : ICurrentParticipant
{
    public Guid Id => Guid.Parse(context.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public Guid SessionId => Guid.Parse(context.HttpContext!.User.FindFirstValue("session_id")!);
    public string EmailAddress => context.HttpContext!.User.FindFirstValue(ClaimTypes.Email)!;
    public bool IsAdministrator => context.HttpContext!.User.IsInRole(AdministrationPolicy.Role);
}
