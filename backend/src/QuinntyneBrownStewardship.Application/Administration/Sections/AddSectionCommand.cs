using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Sections;

public sealed record AddSectionCommand(Guid ModuleId, string Title, string Reading) : IRequest<Guid>;
