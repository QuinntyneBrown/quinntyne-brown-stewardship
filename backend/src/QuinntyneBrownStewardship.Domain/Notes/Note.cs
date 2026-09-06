namespace QuinntyneBrownStewardship.Domain.Notes;

public sealed class Note
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EnrollmentId { get; set; }
    public Guid? ModuleId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? PromptId { get; set; }
    public string Body { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset RevisedAt { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
}
