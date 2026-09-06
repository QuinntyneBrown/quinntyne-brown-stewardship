namespace QuinntyneBrownStewardship.Domain.Scheduling;

public sealed class AvailabilitySlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MentorId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; } = 45;
}
