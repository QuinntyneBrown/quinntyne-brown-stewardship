using MediatR;
using Microsoft.Extensions.Options;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class GetCurriculaQueryHandler(ICurriculumStore store, IOptions<CurriculumOptions> options) : IRequestHandler<GetCurriculaQuery, CurriculaResponse>
{
    public async Task<CurriculaResponse> Handle(GetCurriculaQuery request, CancellationToken ct)
    {
        var cohorts = await store.CohortCounts(ct);
        var programmes = (await store.Curricula(ct)).OrderBy(x => x.CreatedAt).ThenBy(x => x.Key)
            .Select(x => new CurriculumSummary(x.Id, x.Key, x.Title, x.State.ToString(), x.Modules.Count, x.Modules.Count(m => m.State == PublicationState.Published), cohorts.GetValueOrDefault(x.Id), x.CreatedAt, x.PublishedAt)).ToList();
        return new(programmes, AuthoringLimits.From(options.Value));
    }
}
