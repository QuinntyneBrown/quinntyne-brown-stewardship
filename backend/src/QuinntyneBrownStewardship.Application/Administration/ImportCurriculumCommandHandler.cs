using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class ImportCurriculumCommandHandler(IProgrammeStore store, ISystemClock clock) : IRequestHandler<ImportCurriculumCommand, int>
{
    public async Task<int> Handle(ImportCurriculumCommand request, CancellationToken ct)
    {
        return await store.Transaction(async token =>
        {
            // An import bootstraps one draft programme; everything after that happens on the authoring screens.
            if (await store.CurriculumByKey(request.Document.Key, token) != null)
                throw new ProgrammeException(409, $"The key {request.Document.Key} is already used by another programme. Author further changes on the authoring screens.");
            var curriculum = new Curriculum { Key = request.Document.Key, Title = request.Document.Title ?? request.Document.Key, CreatedAt = clock.UtcNow };
            store.Add(curriculum);
            foreach (var source in request.Document.Modules)
            {
                var module = new CurriculumModule { Id = source.Id, CurriculumId = curriculum.Id, Ordinal = source.Ordinal, Title = source.Title, Summary = source.Summary, EffortEstimate = source.EffortEstimate, PracticeSteps = source.PracticeSteps };
                store.Add(module);
                foreach (var section in source.Sections)
                    store.Add(new ModuleSection { Id = section.Id, ModuleId = module.Id, Ordinal = section.Ordinal, Title = section.Title, Reading = section.Reading, CreatedAt = clock.UtcNow });
                foreach (var prompt in source.PreparationPrompts)
                    store.Add(new PreparationPrompt { Id = prompt.Id, ModuleId = module.Id, Ordinal = prompt.Ordinal, Text = prompt.Text });
            }
            return request.Document.Modules.Count;
        }, ct);
    }
}
