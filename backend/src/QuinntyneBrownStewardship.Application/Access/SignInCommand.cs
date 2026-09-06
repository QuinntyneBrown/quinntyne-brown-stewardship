using MediatR;
namespace QuinntyneBrownStewardship.Application.Access;

public sealed record SignInCommand(string EmailAddress, string Password) : IRequest<SignInResponse>;
