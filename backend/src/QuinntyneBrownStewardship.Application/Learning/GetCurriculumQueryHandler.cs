using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Learning;

public sealed class GetCurriculumQueryHandler(IProgrammeStore store, ICurrentParticipant participant, ISystemClock clock, ProgrammeReader reader) : IRequestHandler<GetCurriculumQuery, CurriculumResponse>
{
    public async Task<CurriculumResponse> Handle(GetCurriculumQuery request, CancellationToken ct)
    {
        var enrollment = await store.Enrollment(participant.Id, ct);
        if (enrollment == null) return new(false, null, [], 0, 0, 0, null, null);
        var modules = await store.Modules(enrollment.Cohort.CurriculumKey, ct);
        var completions = await store.Completions(enrollment.Id, ct);
        var current = Progress.Current(modules, completions);
        var items = modules.Select(x => new ModulePathItem(x.Id, x.Ordinal, x.Title, x.Summary, Progress.Complete(x, completions) ? "Complete" : x.Ordinal == current ? "Current" : "Locked")).ToList();
        var completed = items.Count(x => x.State == "Complete");
        var next = (await store.Bookings(enrollment.Id, ct)).Where(x => x.CancelledAt == null && x.Slot.StartsAt > clock.UtcNow).OrderBy(x => x.Slot.StartsAt).FirstOrDefault();
        return new(true, reader.Summary(enrollment.Cohort), items, completed, modules.Count - completed, Progress.Percent(completed, modules.Count), current, next == null ? null : reader.Booking(next, enrollment.Cohort, modules, completions));
    }
}
