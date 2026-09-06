namespace QuinntyneBrownStewardship.Application.Access;

public sealed record SignInResponse(int StatusCode, string? Message = null, DateTimeOffset? RetryAt = null);
