namespace QuinntyneBrownStewardship.Domain.Access;

public sealed class SignInAttempt
{
    public long Id { get; set; }
    public string NormalizedEmail { get; set; } = "";
    public string Origin { get; set; } = "";
    public DateTimeOffset At { get; set; }
    public bool Succeeded { get; set; }
    public bool Refused { get; set; }
}
