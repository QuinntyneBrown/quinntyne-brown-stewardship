using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed class GetAvailabilityQueryHandler(IProgrammeStore store, ISystemClock clock, ProgrammeReader reader) : IRequestHandler<GetAvailabilityQuery, AvailabilityResponse>
{
    public async Task<AvailabilityResponse> Handle(GetAvailabilityQuery request, CancellationToken ct)
    {
        var enrollment = await reader.Enrollment(ct);
        var cohort = enrollment.Cohort;
        var summary = reader.Summary(cohort);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(cohort.TimeZone);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, zone).DateTime);
        var day = request.Day ?? (today < cohort.StartDate ? cohort.StartDate : today >= cohort.EndDate ? cohort.EndDate.AddDays(-1) : today);
        if (day < cohort.StartDate || day >= cohort.EndDate) throw new FluentValidation.ValidationException([new("Day", "Choose a day within your cohort.")]);
        var week = day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
        var days = Enumerable.Range(0, 7).Select(i => week.AddDays(i)).ToList();
        var claimed = await store.ClaimedSlots(cohort.MentorId ?? Guid.Empty, ct);
        var slots = (await store.Slots(cohort.MentorId ?? Guid.Empty, ct)).Where(x => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.StartsAt, zone).DateTime) == day)
            .Select(x => new SlotResponse(x.Id, x.StartsAt, x.DurationMinutes, claimed.Contains(x.Id) || x.StartsAt <= clock.UtcNow ? "Taken" : "Open")).ToList();
        var bookings = await store.Bookings(enrollment.Id, ct);
        var count = bookings.Count(x => x.CancelledAt == null);
        var next = bookings.Where(x => x.CancelledAt == null && x.Slot.StartsAt > clock.UtcNow).OrderBy(x => x.Slot.StartsAt).FirstOrDefault();
        var reason = summary.HasEnded ? "This cohort has ended. New bookings are unavailable." : next != null ? "You already hold a future session." : count >= cohort.SessionAllowance ? "All six sessions in this cohort have been used." : null;
        var modules = await store.ProgressModules(cohort.CurriculumKey, ct);
        var completions = await store.Completions(enrollment.Id, ct);
        return new(summary, week, day, days, slots, count, cohort.SessionAllowance, reason, next == null ? null : reader.Booking(next, cohort, modules, completions),
            bookings.Where(x => x.CancelledAt == null && x.Slot.StartsAt <= clock.UtcNow).OrderByDescending(x => x.Slot.StartsAt).Select(x => reader.Booking(x, cohort, modules, completions)).ToList());
    }
}
