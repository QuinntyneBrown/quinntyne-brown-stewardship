namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record PreparationResponse(BookingResponse Session, int? ModuleOrdinal, string? ModuleTitle, List<PromptResponse> Prompts, List<NoteResponse> Notes, Guid? ModuleId, string? NotesCursor);
