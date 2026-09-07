using QuinntyneBrownStewardship.Application.Administration.Curricula;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed record ModuleDraftResponse(Guid Id, Guid CurriculumId, string CurriculumKey, string CurriculumTitle, int Ordinal, int ModuleCount, string Title, string Summary, string EffortEstimate, List<string> PracticeSteps, string State, Guid Revision,
    int CompletionCount, int NoteCount, int AnswerCount, bool CanRemove, string? RemovalReason, List<SectionDraftSummary> Sections, List<PromptDraftResponse> Prompts, AuthoringLimits Limits);
