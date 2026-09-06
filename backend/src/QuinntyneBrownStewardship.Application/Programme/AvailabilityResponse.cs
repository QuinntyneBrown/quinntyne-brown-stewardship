namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record AvailabilityResponse(CohortSummary Cohort, DateOnly WeekStart, DateOnly SelectedDay, List<DateOnly> Days, List<SlotResponse> Slots, int BookedCount, int Allowance, string? BookingReason, BookingResponse? NextSession, List<BookingResponse> History);
