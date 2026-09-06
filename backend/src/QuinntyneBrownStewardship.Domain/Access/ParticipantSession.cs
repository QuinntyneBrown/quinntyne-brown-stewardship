namespace QuinntyneBrownStewardship.Domain.Access;

public sealed class ParticipantSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParticipantId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset LastActivityAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public bool IsExpired(DateTimeOffset now, TimeSpan idleTimeout) => RevokedAt != null || now - LastActivityAt >= idleTimeout;
    public void Touch(DateTimeOffset now) => LastActivityAt = now;
    public void Revoke(DateTimeOffset now) => RevokedAt = now;
}
