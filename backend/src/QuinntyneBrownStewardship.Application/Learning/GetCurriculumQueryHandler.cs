using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Learning;

public sealed class GetCurriculumQueryHandler(IProgrammeStore store, ICurrentParticipant participant, ISystemClock clock, ProgrammeReader reader) : IRequestHandler<GetCurriculumQuery, CurriculumResponse>
{
    public async Task<CurriculumResponse> Handle(GetCurriculumQuery request, CancellationToken ct)
    {
        var enrollment = await store.Enrollment(participant.Id, ct);
        if (enrollment == null) return new(false, false, null, [], 0, 0, 0, null, null);
        var cohort = enrollment.Cohort;
        // A draft programme publishes no module, so the path and every total read as empty until publication.
        var modules = await store.PublishedProgressModules(cohort.CurriculumId, ct);
        var completions = await store.Completions(enrollment.Id, ct);
        var current = Progress.Current(modules, completions);
        var items = modules.Select(x => new ModulePathItem(x.Id, x.Ordinal, x.Title, x.Summary, Progress.Complete(x, completions) ? "Complete" : x.Ordinal == current ? "Current" : "Locked")).ToList();
        var completed = items.Count(x => x.State == "Complete");
        var next = (await store.Bookings(enrollment.Id, ct)).Where(x => x.CancelledAt == null && x.Slot.StartsAt > clock.UtcNow).OrderBy(x => x.Slot.StartsAt).FirstOrDefault();
        return new(true, cohort.Curriculum.IsPublished, reader.Summary(cohort), items, completed, modules.Count - completed, Progress.Percent(completed, modules.Count), current, next == null ? null : reader.Booking(next, cohort, modules, completions));
    }
}
