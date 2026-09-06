using Microsoft.AspNetCore.Mvc;
using QuinntyneBrownStewardship.Application.Access;
namespace QuinntyneBrownStewardship.Api.Http;

public static class SignInResponseExtensions
{
    public static IActionResult ToActionResult(this SignInResponse response) => new ObjectResult(response) { StatusCode = response.StatusCode };
}
