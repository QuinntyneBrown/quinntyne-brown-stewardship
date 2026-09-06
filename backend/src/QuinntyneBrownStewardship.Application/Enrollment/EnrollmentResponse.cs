namespace QuinntyneBrownStewardship.Application.Enrollment;

public sealed record EnrollmentResponse(bool IsEnrolled, Guid? CohortId = null, string? MentorName = null,
    DateOnly? StartDate = null, DateOnly? EndDate = null, int? CurrentWeek = null, int? SessionAllowance = null, bool? HasEnded = null);
