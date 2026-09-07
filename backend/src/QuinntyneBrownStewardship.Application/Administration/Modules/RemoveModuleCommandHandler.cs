using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Curricula;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

// Removal is refused by the records that depend on the module, named, rather than by the constraints that hold them.
public sealed class RemoveModuleCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<RemoveModuleCommand>
{
    public Task Handle(RemoveModuleCommand request, CancellationToken ct) => store.Transaction<object?>(async token =>
    {
        var module = await store.RequiredModule(request.Id, token);
        var dependents = await store.Dependents(module.Id, token);
        if (dependents.Any) throw new ProgrammeException(409, dependents.Reason!);
        var curriculum = await store.Required(module.CurriculumId, token);
        foreach (var section in module.Sections) store.Remove(section);
        foreach (var prompt in module.PreparationPrompts) store.Remove(prompt);
        store.Remove(module);
        await store.Flush(token);
        OrdinalSequence.Compact(curriculum.Modules.Where(x => x.Id != module.Id).ToList());
        auditor.Record("ModuleRemoved", module.Id);
        return null;
    }, ct);
}
