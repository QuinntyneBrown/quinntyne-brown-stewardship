namespace QuinntyneBrownStewardship.Domain.Learning;

public sealed class CurriculumModule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CurriculumKey { get; set; } = "starter";
    public int Ordinal { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string EffortEstimate { get; set; } = "";
    public List<string> PracticeSteps { get; set; } = [];
    public List<ModuleSection> Sections { get; set; } = [];
    public List<PreparationPrompt> PreparationPrompts { get; set; } = [];
}
