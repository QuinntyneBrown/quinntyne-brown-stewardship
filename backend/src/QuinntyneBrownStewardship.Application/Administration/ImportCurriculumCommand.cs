using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed record ImportCurriculumCommand(CurriculumImport Document) : IRequest<int>;
