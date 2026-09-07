namespace QuinntyneBrownStewardship.Domain.Learning;

public sealed class Curriculum
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public PublicationState State { get; set; } = PublicationState.Draft;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public List<CurriculumModule> Modules { get; set; } = [];
    public bool IsPublished => State == PublicationState.Published;
}
