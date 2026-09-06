namespace QuinntyneBrownStewardship.Domain.Scheduling;

public sealed class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EnrollmentId { get; set; }
    public Guid SlotId { get; set; }
    public AvailabilitySlot Slot { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}
