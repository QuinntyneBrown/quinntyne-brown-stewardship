namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record PromptResponse(Guid Id, string Text, NoteResponse? Answer);
