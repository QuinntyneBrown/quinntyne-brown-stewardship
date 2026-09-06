namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record NoteResponse(Guid Id, Guid? ModuleId, Guid? SessionId, Guid? PromptId, string AttachmentTitle, string Body, DateTimeOffset RevisedAt, Guid Revision, bool CanEdit);
