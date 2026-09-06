namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record SectionResponse(Guid Id, int Ordinal, string Title, string Reading, bool IsComplete);
