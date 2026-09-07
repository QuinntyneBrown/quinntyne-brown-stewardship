namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed record PromptDraftResponse(Guid Id, int Ordinal, string Text, int AnswerCount, bool CanRemove);
