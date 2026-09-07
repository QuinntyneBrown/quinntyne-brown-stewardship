using MediatR;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

// The practice steps travel whole: their order and their number are the list as submitted.
public sealed record ReviseModuleCommand(Guid Id, string Title, string Summary, string EffortEstimate, List<string> PracticeSteps, Guid Revision) : IRequest<RevisionResponse>;
