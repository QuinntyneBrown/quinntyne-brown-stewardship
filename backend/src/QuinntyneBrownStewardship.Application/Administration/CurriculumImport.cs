using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed record CurriculumImport(string Key, string? Title, List<CurriculumModule> Modules);
