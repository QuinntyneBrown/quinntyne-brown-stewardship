using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class RenameCurriculumCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<RenameCurriculumCommand>
{
    public Task Handle(RenameCurriculumCommand request, CancellationToken ct) => store.Transaction<object?>(async token =>
    {
        var curriculum = await store.Required(request.Id, token);
        curriculum.Title = request.Title;
        auditor.Record("CurriculumRenamed", curriculum.Id);
        return null;
    }, ct);
}
