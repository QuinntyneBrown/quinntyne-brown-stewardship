namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record NotesResponse(List<NoteResponse> Notes, List<NoteAttachmentOption> Attachments, int MaxLength, string? NextCursor);
