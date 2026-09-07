using QuinntyneBrownStewardship.Application.Administration.Curricula;
namespace QuinntyneBrownStewardship.Application.Administration.Sections;

public sealed record SectionDraftResponse(Guid Id, Guid ModuleId, int ModuleOrdinal, string ModuleTitle, Guid CurriculumId, string CurriculumTitle, int Ordinal, int SectionCount, string Title, string Reading, Guid Revision, int CompletionCount, bool CanRemove, DateTimeOffset CreatedAt, List<SectionSibling> Siblings, AuthoringLimits Limits);
