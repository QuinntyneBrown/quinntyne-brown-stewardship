namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record CohortSummary(Guid Id, string MentorName, string TimeZone, DateOnly StartDate, DateOnly EndDate, int CurrentWeek, bool HasEnded, int SessionAllowance);
