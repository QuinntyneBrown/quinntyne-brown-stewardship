namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class CurriculumOptions
{
    public int KeyMaxLength { get; set; } = 100;
    public int TitleMaxLength { get; set; } = 120;
    public int SummaryMaxLength { get; set; } = 400;
    public int EffortEstimateMaxLength { get; set; } = 60;
    public int StepMaxLength { get; set; } = 400;
    public int PromptMaxLength { get; set; } = 400;
    public int ReadingMaxLength { get; set; } = 12000;
}
