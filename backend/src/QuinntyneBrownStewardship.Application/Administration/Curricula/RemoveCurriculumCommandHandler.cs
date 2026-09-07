using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class RemoveCurriculumCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<RemoveCurriculumCommand>
{
    public Task Handle(RemoveCurriculumCommand request, CancellationToken ct) => store.Transaction<object?>(async token =>
    {
        var curriculum = await store.Required(request.Id, token);
        var cohorts = (await store.CohortsFollowing(curriculum.Id, token)).Count;
        if (cohorts > 0) throw new ProgrammeException(409, $"{KeyRules.Following(cohorts)} It cannot be removed while {(cohorts == 1 ? "it does" : "they do")}.");
        foreach (var module in curriculum.Modules)
        {
            foreach (var section in module.Sections) store.Remove(section);
            foreach (var prompt in module.PreparationPrompts) store.Remove(prompt);
            store.Remove(module);
        }
        store.Remove(curriculum);
        auditor.Record("CurriculumRemoved", curriculum.Id);
        return null;
    }, ct);
}
