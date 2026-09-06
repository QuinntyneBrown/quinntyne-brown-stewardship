namespace QuinntyneBrownStewardship.Domain.Learning;

public sealed class ModuleSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ModuleId { get; set; }
    public int Ordinal { get; set; }
    public string Title { get; set; } = "";
    public string Reading { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
