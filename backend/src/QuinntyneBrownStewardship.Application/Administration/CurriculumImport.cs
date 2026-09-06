using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed record CurriculumImport(string Key, List<CurriculumModule> Modules);
