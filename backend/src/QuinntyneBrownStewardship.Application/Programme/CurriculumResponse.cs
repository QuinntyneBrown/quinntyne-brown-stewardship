namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record CurriculumResponse(bool IsEnrolled, bool IsProgrammePublished, CohortSummary? Cohort, List<ModulePathItem> Modules, int Completed, int Remaining, int Percent, int? CurrentOrdinal, BookingResponse? NextSession);
