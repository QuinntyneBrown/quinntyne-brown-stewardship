namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record NotePageResponse(List<NoteResponse> Notes, string? NextCursor);
