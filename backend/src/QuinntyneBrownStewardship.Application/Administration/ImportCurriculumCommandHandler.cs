using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class ImportCurriculumCommandHandler(IProgrammeStore store, ISystemClock clock) : IRequestHandler<ImportCurriculumCommand, int>
{
    public async Task<int> Handle(ImportCurriculumCommand request, CancellationToken ct)
    {
        return await store.Transaction(async token =>
        {
            var existing = await store.Modules(request.Document.Key, token);
            foreach (var old in existing)
            {
                var next = request.Document.Modules.SingleOrDefault(x => x.Id == old.Id);
                if (next == null || next.Ordinal != old.Ordinal || old.Sections.Any(s => !next.Sections.Any(n => n.Id == s.Id && n.Ordinal == s.Ordinal)) || old.PreparationPrompts.Any(p => !next.PreparationPrompts.Any(n => n.Id == p.Id && n.Ordinal == p.Ordinal)))
                    throw new ProgrammeException(409, "Imports preserve module, section and prompt identifiers and order. Append content instead of removing or replacing it.");
            }
            foreach (var source in request.Document.Modules)
            {
                var module = existing.SingleOrDefault(x => x.Id == source.Id);
                if (module == null) { module = new() { Id = source.Id, CurriculumKey = request.Document.Key, Ordinal = source.Ordinal }; store.Add(module); }
                module.Title = source.Title; module.Summary = source.Summary; module.EffortEstimate = source.EffortEstimate; module.PracticeSteps = source.PracticeSteps;
                foreach (var sourceSection in source.Sections)
                {
                    var section = module.Sections.SingleOrDefault(x => x.Id == sourceSection.Id);
                    if (section == null) { section = new() { Id = sourceSection.Id, ModuleId = module.Id, Ordinal = sourceSection.Ordinal, CreatedAt = clock.UtcNow }; module.Sections.Add(section); store.Add(section); }
                    section.Title = sourceSection.Title; section.Reading = sourceSection.Reading;
                }
                foreach (var sourcePrompt in source.PreparationPrompts)
                {
                    var prompt = module.PreparationPrompts.SingleOrDefault(x => x.Id == sourcePrompt.Id);
                    if (prompt == null) { prompt = new() { Id = sourcePrompt.Id, ModuleId = module.Id, Ordinal = sourcePrompt.Ordinal }; module.PreparationPrompts.Add(prompt); store.Add(prompt); }
                    prompt.Text = sourcePrompt.Text;
                }
            }
            return request.Document.Modules.Count;
        }, ct);
    }
}
