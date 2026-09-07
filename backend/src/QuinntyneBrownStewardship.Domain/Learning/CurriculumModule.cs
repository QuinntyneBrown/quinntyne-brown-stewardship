namespace QuinntyneBrownStewardship.Domain.Learning;

public sealed class CurriculumModule : IOrdered
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CurriculumId { get; set; }
    public int Ordinal { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string EffortEstimate { get; set; } = "";
    public List<string> PracticeSteps { get; set; } = [];
    public PublicationState State { get; set; } = PublicationState.Draft;
    public Guid Revision { get; set; } = Guid.NewGuid();
    public List<ModuleSection> Sections { get; set; } = [];
    public List<PreparationPrompt> PreparationPrompts { get; set; } = [];
}
