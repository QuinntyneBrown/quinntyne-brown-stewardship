namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record CurriculumDraftResponse(Guid Id, string Key, string Title, string State, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt, int CohortCount, bool CanChangeKey, bool CanRemove, List<ModuleDraftSummary> Modules, ReadinessResponse Readiness, AuthoringLimits Limits);
