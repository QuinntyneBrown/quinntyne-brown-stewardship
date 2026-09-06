namespace QuinntyneBrownStewardship.Domain.Enrollment;

public sealed class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParticipantId { get; set; }
    public Guid CohortId { get; set; }
    public bool IsActive { get; set; } = true;
    public Cohort Cohort { get; set; } = null!;
}
