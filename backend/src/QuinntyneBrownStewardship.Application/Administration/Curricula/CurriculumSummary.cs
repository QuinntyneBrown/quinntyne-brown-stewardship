namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record CurriculumSummary(Guid Id, string Key, string Title, string State, int ModuleCount, int PublishedModuleCount, int CohortCount, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt);
