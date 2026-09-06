using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Scheduling;
namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed class BookingOperations(IProgrammeStore store, ICurrentParticipant participant, ISystemClock clock, ICorrelationContext correlation, ProgrammeReader reader)
{
    public Task<BookingResponse> Execute(Guid? bookingId, Guid? slotId, CancellationToken ct) => store.Transaction(async token =>
    {
        var enrollment = await reader.Enrollment(token);
        var cohort = enrollment.Cohort;
        var bookings = await store.Bookings(enrollment.Id, token);
        var booking = bookingId == null ? null : bookings.SingleOrDefault(x => x.Id == bookingId) ?? throw new ProgrammeException(404, "Session not found.");
        if (booking != null && !(slotId == null && booking.CancelledAt != null) && (booking.CancelledAt != null || booking.Slot.StartsAt - clock.UtcNow <= TimeSpan.FromHours(24)))
            throw new ProgrammeException(409, "Changes close 24 hours before the session starts.");
        Guid? previousSlot = booking?.SlotId;
        var changed = true;
        if (slotId == null)
        {
            if (booking == null) throw new ProgrammeException(404, "Session not found.");
            changed = booking.CancelledAt == null;
            booking.CancelledAt ??= clock.UtcNow;
        }
        else
        {
            var summary = reader.Summary(cohort);
            if (summary.HasEnded) throw new ProgrammeException(409, "This cohort has ended. New bookings are unavailable.");
            if (booking == null && bookings.Any(x => x.CancelledAt == null && x.Slot.StartsAt > clock.UtcNow))
                throw new ProgrammeException(409, "You already hold a future session. Reschedule or cancel it first.");
            if (booking == null && bookings.Count(x => x.CancelledAt == null) >= cohort.SessionAllowance)
                throw new ProgrammeException(409, "All six sessions in this cohort have been used.");
            var slot = (await store.Slots(cohort.MentorId ?? Guid.Empty, token)).SingleOrDefault(x => x.Id == slotId)
                ?? throw new ProgrammeException(404, "Slot not found.");
            var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(slot.StartsAt, TimeZoneInfo.FindSystemTimeZoneById(cohort.TimeZone)).DateTime);
            if (slot.StartsAt <= clock.UtcNow || day < cohort.StartDate || day >= cohort.EndDate)
                throw new ProgrammeException(409, "That slot is outside the available booking period.");
            if (booking?.SlotId != slot.Id && (await store.ClaimedSlots(slot.MentorId, token)).Contains(slot.Id))
                throw new ProgrammeException(409, "That slot is no longer available. Choose another time.");
            if (booking == null)
            {
                booking = new() { EnrollmentId = enrollment.Id, SlotId = slot.Id, Slot = slot, CreatedAt = clock.UtcNow };
                store.Add(booking);
            }
            else
            {
                changed = booking.SlotId != slot.Id;
                booking.SlotId = slot.Id; booking.Slot = slot;
            }
        }
        if (changed) store.Add(new BookingAudit { BookingId = booking.Id, ActorId = participant.Id, Action = slotId == null ? "Cancelled" : bookingId == null ? "Booked" : "Rescheduled", PreviousSlotId = previousSlot, SlotId = slotId, At = clock.UtcNow, CorrelationId = correlation.Id });
        return reader.Booking(booking, cohort, await store.Modules(cohort.CurriculumKey, token), await store.Completions(enrollment.Id, token));
    }, ct);
}
