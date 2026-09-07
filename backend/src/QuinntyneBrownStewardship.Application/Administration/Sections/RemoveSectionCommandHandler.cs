using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Modules;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Sections;

public sealed class RemoveSectionCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<RemoveSectionCommand>
{
    public Task Handle(RemoveSectionCommand request, CancellationToken ct) => store.Transaction<object?>(async token =>
    {
        var section = await store.RequiredSection(request.Id, token);
        var completions = await store.CompletionCount(section.Id, token);
        if (completions > 0) throw new ProgrammeException(409, $"{completions} completion{(completions == 1 ? " is" : "s are")} recorded against this section. It cannot be removed while {(completions == 1 ? "that record stands" : "those records stand")}.");
        var module = await store.RequiredModule(section.ModuleId, token);
        store.Remove(section);
        await store.Flush(token);
        OrdinalSequence.Compact(module.Sections.Where(x => x.Id != section.Id).ToList());
        auditor.Record("SectionRemoved", section.Id);
        return null;
    }, ct);
}
