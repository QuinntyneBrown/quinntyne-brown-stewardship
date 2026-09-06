namespace QuinntyneBrownStewardship.Domain.Scheduling;

public sealed class BookingAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid ActorId { get; set; }
    public Guid? PreviousSlotId { get; set; }
    public Guid? SlotId { get; set; }
    public string Action { get; set; } = "";
    public string CorrelationId { get; set; } = "";
    public DateTimeOffset At { get; set; }
}
