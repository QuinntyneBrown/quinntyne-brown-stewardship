namespace QuinntyneBrownStewardship.Application.Access;

public sealed class SignInOptions
{
    public int AccountLimit { get; set; } = 10;
    public int OriginLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(15);
}
