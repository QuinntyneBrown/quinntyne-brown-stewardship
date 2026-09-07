namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record CohortFollower(Guid Id, DateOnly StartDate, DateOnly EndDate, int DurationWeeks, int SessionCadenceWeeks, int SessionAllowance, bool HasEnded);
