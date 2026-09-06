namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record BookingResponse(Guid Id, Guid SlotId, DateTimeOffset StartsAt, int DurationMinutes, string MentorName, string TimeZone, string Status, bool CanChange, string? ChangeReason, int? ModuleOrdinal, string? ModuleTitle);
