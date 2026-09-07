using MediatR;
using Microsoft.Extensions.Options;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Curricula;
using QuinntyneBrownStewardship.Application.Administration.Modules;
using QuinntyneBrownStewardship.Application.Common;
namespace QuinntyneBrownStewardship.Application.Administration.Sections;

public sealed class GetSectionDraftQueryHandler(ICurriculumStore store, IOptions<CurriculumOptions> options) : IRequestHandler<GetSectionDraftQuery, SectionDraftResponse>
{
    public async Task<SectionDraftResponse> Handle(GetSectionDraftQuery request, CancellationToken ct)
    {
        var section = await store.RequiredSection(request.Id, ct);
        var module = await store.RequiredModule(section.ModuleId, ct);
        var curriculum = await store.Curriculum(module.CurriculumId, ct) ?? throw new ProgrammeException(404, "Programme not found.");
        var completions = await store.CompletionCount(section.Id, ct);
        return new(section.Id, module.Id, module.Ordinal, module.Title, curriculum.Id, curriculum.Title, section.Ordinal, module.Sections.Count, section.Title, section.Reading, section.Revision, completions, completions == 0, section.CreatedAt,
            module.Sections.OrderBy(x => x.Ordinal).Select(x => new SectionSibling(x.Id, x.Ordinal, x.Title)).ToList(), AuthoringLimits.From(options.Value));
    }
}
