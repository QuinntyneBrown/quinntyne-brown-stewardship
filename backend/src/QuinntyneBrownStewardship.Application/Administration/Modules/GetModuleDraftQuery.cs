using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed record GetModuleDraftQuery(Guid Id) : IRequest<ModuleDraftResponse>;
