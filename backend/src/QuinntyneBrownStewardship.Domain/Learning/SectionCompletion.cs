namespace QuinntyneBrownStewardship.Domain.Learning;

public sealed class SectionCompletion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EnrollmentId { get; set; }
    public Guid SectionId { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}
