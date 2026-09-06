namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record ModuleResponse(Guid Id, int Ordinal, string Title, string Summary, string EffortEstimate, List<string> PracticeSteps, List<SectionResponse> Sections, List<PromptResponse> PreparationPrompts, int CompletedSections, int Percent, Guid ResumeSectionId, bool IsComplete, List<NoteResponse> Notes);
