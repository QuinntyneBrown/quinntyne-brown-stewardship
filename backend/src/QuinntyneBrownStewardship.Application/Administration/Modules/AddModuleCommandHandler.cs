using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Curricula;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

// A new module takes the last position and starts in draft, so it disturbs no participant until the next publication.
public sealed class AddModuleCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<AddModuleCommand, Guid>
{
    public Task<Guid> Handle(AddModuleCommand request, CancellationToken ct) => store.Transaction(async token =>
    {
        var curriculum = await store.Required(request.CurriculumId, token);
        var module = new CurriculumModule { CurriculumId = curriculum.Id, Ordinal = OrdinalSequence.Next(curriculum.Modules), Title = request.Title, Summary = request.Summary };
        store.Add(module);
        auditor.Record("ModuleAdded", module.Id);
        return module.Id;
    }, ct);
}
