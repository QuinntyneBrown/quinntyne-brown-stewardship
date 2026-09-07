namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

// Whether the programme may be published, and what a publication would reach and move.
public sealed record ReadinessResponse(bool CanPublish, string? Reason, List<string> EmptyModuleTitles, int UnpublishedModuleCount, int ActiveCohortCount, int ParticipantsMovedBack, int? MovedBackToOrdinal, List<CohortFollower> Cohorts);
