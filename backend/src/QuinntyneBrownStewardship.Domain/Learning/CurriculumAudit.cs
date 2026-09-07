namespace QuinntyneBrownStewardship.Domain.Learning;

public sealed class CurriculumAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorId { get; set; }
    public string Action { get; set; } = "";
    public Guid TargetId { get; set; }
    public string CorrelationId { get; set; } = "";
    public DateTimeOffset At { get; set; }
}
