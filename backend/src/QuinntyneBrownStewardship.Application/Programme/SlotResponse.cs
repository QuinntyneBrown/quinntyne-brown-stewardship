namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record SlotResponse(Guid Id, DateTimeOffset StartsAt, int DurationMinutes, string State);
