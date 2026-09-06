using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Domain.Scheduling;
using QuinntyneBrownStewardship.Domain.Notes;
namespace QuinntyneBrownStewardship.Application.Programme;

public sealed class ProgrammeReader(IProgrammeStore store, ICurrentParticipant participant, ISystemClock clock)
{
    public async Task<QuinntyneBrownStewardship.Domain.Enrollment.Enrollment> Enrollment(CancellationToken ct)
        => await store.Enrollment(participant.Id, ct) ?? throw new ProgrammeException(404, "You are not enrolled in a cohort yet.");
    public CohortSummary Summary(Cohort cohort)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(cohort.TimeZone)).DateTime);
        return new(cohort.Id, cohort.MentorName, cohort.TimeZone, cohort.StartDate, cohort.EndDate, cohort.CurrentWeek(today), cohort.HasEnded(today), cohort.SessionAllowance);
    }
    public async Task<ModuleResponse> Module(int? ordinal, CancellationToken ct)
    {
        var enrollment = await Enrollment(ct);
        var modules = await store.Modules(enrollment.Cohort.CurriculumKey, ct);
        var completions = await store.Completions(enrollment.Id, ct);
        var current = Progress.Current(modules, completions);
        var module = modules.SingleOrDefault(x => x.Ordinal == (ordinal ?? current ?? modules.LastOrDefault()?.Ordinal))
            ?? throw new ProgrammeException(404, "Module not found.");
        if (!Progress.Complete(module, completions) && module.Ordinal != current)
            throw new ProgrammeException(409, "Complete the current module to unlock this module.");
        var sections = module.Sections.OrderBy(x => x.Ordinal).Select(x => new SectionResponse(x.Id, x.Ordinal, x.Title, x.Reading, completions.Any(c => c.SectionId == x.Id))).ToList();
        var count = sections.Count(x => x.IsComplete);
        var notes = await store.Notes(enrollment.Id, ct);
        return new(module.Id, module.Ordinal, module.Title, module.Summary, module.EffortEstimate, module.PracticeSteps, sections,
            module.PreparationPrompts.OrderBy(x => x.Ordinal).Select(p => new PromptResponse(p.Id, p.Text, notes.FirstOrDefault(n => n.PromptId == p.Id) is { } n ? Note(n, module.Title, true) : null)).ToList(),
            count, Progress.Percent(count, sections.Count), (sections.FirstOrDefault(x => !x.IsComplete) ?? sections.Last()).Id, count == sections.Count,
            notes.Where(x => x.ModuleId == module.Id).Select(x => Note(x, module.Title, true)).ToList());
    }
    public BookingResponse Booking(Booking booking, Cohort cohort, List<CurriculumModule> modules, List<SectionCompletion> completions)
    {
        var past = booking.Slot.StartsAt <= clock.UtcNow;
        var module = modules.SingleOrDefault(x => x.Ordinal == Progress.Current(modules, completions, past ? booking.Slot.StartsAt : clock.UtcNow));
        var canChange = booking.CancelledAt == null && booking.Slot.StartsAt - clock.UtcNow > TimeSpan.FromHours(24);
        return new(booking.Id, booking.SlotId, booking.Slot.StartsAt, booking.Slot.DurationMinutes, cohort.MentorName, cohort.TimeZone,
            booking.CancelledAt != null ? "Cancelled" : past ? "Held" : "Booked", canChange,
            canChange ? null : booking.CancelledAt != null ? "This session was cancelled." : "Changes close 24 hours before the session starts.", module?.Ordinal, module?.Title);
    }
    public static NoteResponse Note(Note note, string title, bool canEdit) => new(note.Id, note.ModuleId, note.SessionId, note.PromptId, title, note.Body, note.RevisedAt, note.Revision, canEdit);
    public static string NoteTitle(Note note, List<CurriculumModule> modules, List<Booking> bookings)
        => note.ModuleId is { } moduleId ? modules.Single(x => x.Id == moduleId).Title : "Session · " + bookings.Single(x => x.Id == note.SessionId).Slot.StartsAt.ToString("yyyy-MM-dd HH:mm 'UTC'");
}

