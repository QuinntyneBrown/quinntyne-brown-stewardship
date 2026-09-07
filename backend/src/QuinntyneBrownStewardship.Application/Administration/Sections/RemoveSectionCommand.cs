using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Sections;

public sealed record RemoveSectionCommand(Guid Id) : IRequest;
