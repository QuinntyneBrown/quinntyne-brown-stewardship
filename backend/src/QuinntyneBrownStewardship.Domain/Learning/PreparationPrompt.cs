namespace QuinntyneBrownStewardship.Domain.Learning;

public sealed class PreparationPrompt : IOrdered
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ModuleId { get; set; }
    public int Ordinal { get; set; }
    public string Text { get; set; } = "";
}
