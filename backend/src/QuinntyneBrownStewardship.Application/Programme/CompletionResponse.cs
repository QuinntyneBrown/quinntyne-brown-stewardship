namespace QuinntyneBrownStewardship.Application.Programme;

public sealed record CompletionResponse(bool IsModuleComplete, Guid? NextSectionId, int? CurrentOrdinal);
