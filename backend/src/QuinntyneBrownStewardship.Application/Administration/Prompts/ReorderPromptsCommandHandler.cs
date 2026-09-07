using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Modules;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

public sealed class ReorderPromptsCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<ReorderPromptsCommand>
{
    public Task Handle(ReorderPromptsCommand request, CancellationToken ct) => store.Transaction<object?>(async token =>
    {
        var module = await store.RequiredModule(request.ModuleId, token);
        await Reordering.Apply(store, module.PreparationPrompts, request.Order, "prompts", "module", token);
        auditor.Record("PromptsReordered", module.Id);
        return null;
    }, ct);
}
