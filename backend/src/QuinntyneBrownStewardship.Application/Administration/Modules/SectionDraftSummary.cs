namespace QuinntyneBrownStewardship.Application.Administration.Modules;

// A section row on the module screen carries its size and what depends on it, never its prose.
public sealed record SectionDraftSummary(Guid Id, int Ordinal, string Title, int WordCount, int CompletionCount, bool CanRemove);
