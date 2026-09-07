using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

// Publication is the one act that changes what a participant reads: every module becomes readable at once, and the time is stamped.
public sealed class PublishCurriculumCommandHandler(ICurriculumStore store, CurriculumAuditor auditor, ISystemClock clock) : IRequestHandler<PublishCurriculumCommand, PublicationResponse>
{
    public Task<PublicationResponse> Handle(PublishCurriculumCommand request, CancellationToken ct) => store.Transaction(async token =>
    {
        var curriculum = await store.Required(request.Id, token);
        var readiness = CurriculumReadiness.Of(curriculum);
        if (!readiness.CanPublish) throw new ProgrammeException(409, readiness.Reason!);
        curriculum.State = PublicationState.Published; curriculum.PublishedAt = clock.UtcNow;
        foreach (var module in curriculum.Modules) module.State = PublicationState.Published;
        auditor.Record("CurriculumPublished", curriculum.Id);
        return new PublicationResponse(curriculum.State.ToString(), curriculum.PublishedAt.Value, curriculum.Modules.Count);
    }, ct);
}
