using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class ReorderModulesCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<ReorderModulesCommand>
{
    public Task Handle(ReorderModulesCommand request, CancellationToken ct) => store.Transaction<object?>(async token =>
    {
        var curriculum = await store.Required(request.CurriculumId, token);
        await Reordering.Apply(store, curriculum.Modules, request.Order, "modules", "programme", token);
        auditor.Record("ModulesReordered", curriculum.Id);
        return null;
    }, ct);
}
