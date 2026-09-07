namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record ModuleDraftSummary(Guid Id, int Ordinal, string Title, string Summary, string State, int SectionCount, int StepCount, int PromptCount);
