using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed class ReorderSectionsCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<ReorderSectionsCommand>
{
    public Task Handle(ReorderSectionsCommand request, CancellationToken ct) => store.Transaction<object?>(async token =>
    {
        var module = await store.RequiredModule(request.ModuleId, token);
        await Reordering.Apply(store, module.Sections, request.Order, "sections", "module", token);
        auditor.Record("SectionsReordered", module.Id);
        return null;
    }, ct);
}
