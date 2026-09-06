using MediatR;
using QuinntyneBrownStewardship.Application.Programme;
namespace QuinntyneBrownStewardship.Application.Administration;

public sealed record CreateCohortCommand(Guid Id, DateOnly StartDate, string MentorEmail, string CurriculumKey = "starter", string TimeZone = "America/Toronto") : IRequest<Guid>;
