using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Application.Notes;

namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed class GetPreparationQueryHandler(IProgrammeStore store, ProgrammeReader reader) : IRequestHandler<GetPreparationQuery, PreparationResponse>
{
    public async Task<PreparationResponse> Handle(GetPreparationQuery request, CancellationToken ct)
    {
        var enrollment = await reader.Enrollment(ct);
        var bookings = await store.Bookings(enrollment.Id, ct);
        var booking = bookings.SingleOrDefault(x => x.Id == request.Id) ?? throw new ProgrammeException(404, "Session not found.");
        var modules = await store.Modules(enrollment.Cohort.CurriculumKey, ct);
        var session = reader.Booking(booking, enrollment.Cohort, modules, await store.Completions(enrollment.Id, ct));
        var module = modules.SingleOrDefault(x => x.Ordinal == session.ModuleOrdinal) ?? modules.LastOrDefault();
        var notes = module == null ? [] : await store.PromptAnswers(enrollment.Id, module.Id, ct);
        var page = NotePagination.Page(await store.NotesPage(enrollment.Id, null, booking.Id, null, true, ct), x => ProgrammeReader.Note(x, ProgrammeReader.NoteTitle(x, modules, bookings), true));
        return new(session, module?.Ordinal, module?.Title,
            module?.PreparationPrompts.OrderBy(x => x.Ordinal).Select(p => new PromptResponse(p.Id, p.Text, notes.FirstOrDefault(n => n.PromptId == p.Id) is { } n ? ProgrammeReader.Note(n, module.Title, true) : null)).ToList() ?? [],
            page.Notes, module?.Id, page.NextCursor);
    }
}
