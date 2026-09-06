using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;
using Microsoft.Extensions.Options;
namespace QuinntyneBrownStewardship.Application.Notes;

public sealed class GetNotesQueryHandler(IProgrammeStore store, ProgrammeReader reader, IOptions<NoteOptions> options) : IRequestHandler<GetNotesQuery, NotesResponse>
{
    public async Task<NotesResponse> Handle(GetNotesQuery request, CancellationToken ct)
    {
        var cursor = NoteCursor.Parse(request.Cursor);
        var enrollment = await reader.Enrollment(ct);
        var modules = await store.ProgressModules(enrollment.Cohort.CurriculumKey, ct);
        var completions = await store.Completions(enrollment.Id, ct);
        var current = Progress.Current(modules, completions);
        var bookings = await store.Bookings(enrollment.Id, ct);
        if (request.ModuleId != null && !modules.Any(x => x.Id == request.ModuleId)) throw new ProgrammeException(404, "Module not found.");
        if (request.SessionId != null && !bookings.Any(x => x.Id == request.SessionId)) throw new ProgrammeException(404, "Session not found.");
        var page = NotePagination.Page(await store.NotesPage(enrollment.Id, request.ModuleId, request.SessionId, cursor, request.GeneralOnly, ct), x => ProgrammeReader.Note(x, ProgrammeReader.NoteTitle(x, modules, bookings), true));
        return new(page.Notes,
            modules.Where(x => x.Ordinal == current || Progress.Complete(x, completions)).Select(x => new NoteAttachmentOption(x.Id, null, $"Module {x.Ordinal:00} · {x.Title}"))
            .Concat(bookings.Where(x => x.CancelledAt == null).OrderByDescending(x => x.Slot.StartsAt).Select(x => new NoteAttachmentOption(null, x.Id, "Session · " + TimeZoneInfo.ConvertTime(x.Slot.StartsAt, TimeZoneInfo.FindSystemTimeZoneById(enrollment.Cohort.TimeZone)).ToString("yyyy-MM-dd HH:mm zzz")))).ToList(), options.Value.MaxLength, page.NextCursor);
    }
}
