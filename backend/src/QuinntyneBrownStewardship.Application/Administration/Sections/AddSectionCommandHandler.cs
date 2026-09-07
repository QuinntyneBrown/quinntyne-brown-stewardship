using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Modules;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Sections;

// A new section is placed last and dated now, so a participant who had finished the module finds it reopened without their history rewritten.
public sealed class AddSectionCommandHandler(ICurriculumStore store, CurriculumAuditor auditor, ISystemClock clock) : IRequestHandler<AddSectionCommand, Guid>
{
    public Task<Guid> Handle(AddSectionCommand request, CancellationToken ct) => store.Transaction(async token =>
    {
        var module = await store.RequiredModule(request.ModuleId, token);
        var section = new ModuleSection { ModuleId = module.Id, Ordinal = OrdinalSequence.Next(module.Sections), Title = request.Title, Reading = request.Reading, CreatedAt = clock.UtcNow };
        store.Add(section);
        auditor.Record("SectionAdded", section.Id);
        return section.Id;
    }, ct);
}
