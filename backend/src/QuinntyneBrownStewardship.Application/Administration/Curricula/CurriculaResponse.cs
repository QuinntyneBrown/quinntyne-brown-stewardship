namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed record CurriculaResponse(List<CurriculumSummary> Programmes, AuthoringLimits Limits);
