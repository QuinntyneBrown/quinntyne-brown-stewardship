using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed class GetSessionHistoryQueryHandler(IProgrammeStore store, ISystemClock clock, ProgrammeReader reader) : IRequestHandler<GetSessionHistoryQuery, HistoryResponse>
{
    public async Task<HistoryResponse> Handle(GetSessionHistoryQuery request, CancellationToken ct)
    {
        var enrollment = await reader.Enrollment(ct);
        var bookings = await store.Bookings(enrollment.Id, ct);
        var modules = await store.PublishedModules(enrollment.Cohort.CurriculumId, ct);
        var completions = await store.Completions(enrollment.Id, ct);
        return new(bookings.Where(x => x.CancelledAt == null && x.Slot.StartsAt <= clock.UtcNow).OrderByDescending(x => x.Slot.StartsAt).Select(x => reader.Booking(x, enrollment.Cohort, modules, completions)).ToList(), bookings.Count(x => x.CancelledAt == null), enrollment.Cohort.SessionAllowance);
    }
}
