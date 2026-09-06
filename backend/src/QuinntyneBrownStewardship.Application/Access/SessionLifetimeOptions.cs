namespace QuinntyneBrownStewardship.Application.Access;

public sealed class SessionLifetimeOptions
{
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromDays(30);
}
