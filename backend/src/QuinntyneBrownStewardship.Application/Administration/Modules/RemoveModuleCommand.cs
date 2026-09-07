using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed record RemoveModuleCommand(Guid Id) : IRequest;
