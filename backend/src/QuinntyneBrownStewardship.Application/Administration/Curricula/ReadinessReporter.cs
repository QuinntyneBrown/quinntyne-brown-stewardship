using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

// States, before the act, what a publication would do: whether it can happen, whom it reaches, and whose current module it would move.
public sealed class ReadinessReporter(ICurriculumStore store, ISystemClock clock)
{
    public async Task<ReadinessResponse> Report(Curriculum curriculum, CancellationToken ct)
    {
        var readiness = CurriculumReadiness.Of(curriculum);
        var cohorts = await store.CohortsFollowing(curriculum.Id, ct);
        var followers = cohorts.Select(x => { var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(x.TimeZone)).DateTime); return new CohortFollower(x.Id, x.StartDate, x.EndDate, x.DurationWeeks, x.SessionCadenceWeeks, x.SessionAllowance, x.HasEnded(today)); }).ToList();
        var published = curriculum.Modules.Where(x => x.State == PublicationState.Published).ToList();
        var movedBack = new List<int>();
        if (curriculum.Modules.Count > published.Count)
            foreach (var completions in (await store.CompletionsByEnrollment(curriculum.Id, ct)).Values)
            {
                // A participant with nothing left is beyond every ordinal; a module published below their current one moves them back to it.
                var before = Progress.Current(published, completions) ?? int.MaxValue;
                var after = Progress.Current(curriculum.Modules, completions) ?? int.MaxValue;
                if (after < before) movedBack.Add(after);
            }
        return new(readiness.CanPublish, readiness.Reason, readiness.EmptyModules.Select(CurriculumReadiness.Name).ToList(), curriculum.Modules.Count(x => x.State == PublicationState.Draft),
            followers.Count(x => !x.HasEnded), movedBack.Count, movedBack.Count == 0 ? null : movedBack.Min(), followers);
    }
}
