using MediatR;
using Microsoft.Extensions.Options;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class GetCurriculumDraftQueryHandler(ICurriculumStore store, ReadinessReporter reporter, IOptions<CurriculumOptions> options) : IRequestHandler<GetCurriculumDraftQuery, CurriculumDraftResponse>
{
    public async Task<CurriculumDraftResponse> Handle(GetCurriculumDraftQuery request, CancellationToken ct)
    {
        var curriculum = await store.Curriculum(request.Id, ct) ?? throw new ProgrammeException(404, "Programme not found.");
        var cohorts = (await store.CohortsFollowing(curriculum.Id, ct)).Count;
        return Draft(curriculum, cohorts, await reporter.Report(curriculum, ct), options.Value);
    }
    public static CurriculumDraftResponse Draft(Curriculum curriculum, int cohorts, ReadinessResponse readiness, CurriculumOptions options)
        => new(curriculum.Id, curriculum.Key, curriculum.Title, curriculum.State.ToString(), curriculum.CreatedAt, curriculum.PublishedAt, cohorts, cohorts == 0, cohorts == 0,
            curriculum.Modules.OrderBy(x => x.Ordinal).Select(x => new ModuleDraftSummary(x.Id, x.Ordinal, x.Title, x.Summary, x.State.ToString(), x.Sections.Count, x.PracticeSteps.Count, x.PreparationPrompts.Count)).ToList(),
            readiness, AuthoringLimits.From(options));
}
