using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed class ReviseModuleCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<ReviseModuleCommand, RevisionResponse>
{
    public Task<RevisionResponse> Handle(ReviseModuleCommand request, CancellationToken ct) => store.Transaction(async token =>
    {
        var module = await store.RequiredModule(request.Id, token);
        // The transaction holds the programme lock, so the comparison is exact; the concurrency token is the backstop.
        if (module.Revision != request.Revision) throw new ProgrammeException(409, "This module changed since it was opened. Reload it to see the current content.");
        module.Title = request.Title; module.Summary = request.Summary; module.EffortEstimate = request.EffortEstimate; module.PracticeSteps = request.PracticeSteps;
        module.Revision = Guid.NewGuid();
        auditor.Record("ModuleRevised", module.Id);
        return new RevisionResponse(module.Revision);
    }, ct);
}
