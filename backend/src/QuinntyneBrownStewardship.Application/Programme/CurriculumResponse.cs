namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record CurriculumResponse(bool IsEnrolled, CohortSummary? Cohort, List<ModulePathItem> Modules, int Completed, int Remaining, int Percent, int? CurrentOrdinal, BookingResponse? NextSession);
