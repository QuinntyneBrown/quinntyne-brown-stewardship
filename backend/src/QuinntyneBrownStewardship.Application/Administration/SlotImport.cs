namespace QuinntyneBrownStewardship.Application.Administration;

public sealed record SlotImport(Guid Id, DateTimeOffset StartsAt, int DurationMinutes = 45);
