using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class ChangeCurriculumKeyCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<ChangeCurriculumKeyCommand>
{
    public Task Handle(ChangeCurriculumKeyCommand request, CancellationToken ct) => store.Transaction<object?>(async token =>
    {
        var curriculum = await store.Required(request.Id, token);
        var cohorts = (await store.CohortsFollowing(curriculum.Id, token)).Count;
        if (cohorts > 0) throw new ProgrammeException(409, $"{KeyRules.Following(cohorts)} The key cannot be changed while {(cohorts == 1 ? "it does" : "they do")}.");
        await KeyRules.Free(store, request.Key, curriculum.Id, token);
        curriculum.Key = request.Key;
        auditor.Record("CurriculumRekeyed", curriculum.Id);
        return null;
    }, ct);
}
