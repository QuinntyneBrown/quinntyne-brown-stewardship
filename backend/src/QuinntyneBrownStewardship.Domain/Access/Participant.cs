namespace QuinntyneBrownStewardship.Domain.Access;

public sealed class Participant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmailAddress { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}
