using MediatR;
using Microsoft.Extensions.Options;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Curricula;
using QuinntyneBrownStewardship.Application.Common;
namespace QuinntyneBrownStewardship.Application.Administration.Modules;

public sealed class GetModuleDraftQueryHandler(ICurriculumStore store, IOptions<CurriculumOptions> options) : IRequestHandler<GetModuleDraftQuery, ModuleDraftResponse>
{
    public async Task<ModuleDraftResponse> Handle(GetModuleDraftQuery request, CancellationToken ct)
    {
        var module = await store.RequiredModule(request.Id, ct);
        var curriculum = await store.Curriculum(module.CurriculumId, ct) ?? throw new ProgrammeException(404, "Programme not found.");
        var completions = await store.SectionCompletionCounts(module.Id, ct);
        var answers = await store.PromptAnswerCounts(module.Id, ct);
        var dependents = new ModuleDependents(completions.Values.Sum(), await store.ModuleNoteCount(module.Id, ct), answers.Values.Sum());
        return new(module.Id, curriculum.Id, curriculum.Key, curriculum.Title, module.Ordinal, curriculum.Modules.Count, module.Title, module.Summary, module.EffortEstimate, module.PracticeSteps, module.State.ToString(), module.Revision,
            dependents.Completions, dependents.Notes, dependents.Answers, !dependents.Any, dependents.Reason,
            module.Sections.OrderBy(x => x.Ordinal).Select(s => new SectionDraftSummary(s.Id, s.Ordinal, s.Title, ModuleLookup.WordCount(s.Reading), completions.GetValueOrDefault(s.Id), completions.GetValueOrDefault(s.Id) == 0)).ToList(),
            module.PreparationPrompts.OrderBy(x => x.Ordinal).Select(p => new PromptDraftResponse(p.Id, p.Ordinal, p.Text, answers.GetValueOrDefault(p.Id), answers.GetValueOrDefault(p.Id) == 0)).ToList(),
            AuthoringLimits.From(options.Value));
    }
}
