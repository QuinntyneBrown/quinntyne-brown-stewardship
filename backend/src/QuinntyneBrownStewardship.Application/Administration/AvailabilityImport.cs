namespace QuinntyneBrownStewardship.Application.Administration;

public sealed record AvailabilityImport(string MentorEmail, List<SlotImport> Slots);
