using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Domain.Notes;
namespace QuinntyneBrownStewardship.Application.Notes;

public sealed class SaveNoteCommandHandler(IProgrammeStore store, ProgrammeReader reader, ISystemClock clock) : IRequestHandler<SaveNoteCommand, NoteResponse>
{
    public async Task<NoteResponse> Handle(SaveNoteCommand request, CancellationToken ct)
    {
        return await store.Transaction(async token =>
        {
            var enrollment = await reader.Enrollment(token);
            var modules = await store.PublishedModules(enrollment.Cohort.CurriculumId, token);
            var bookings = await store.Bookings(enrollment.Id, token);
            Note note;
            if (request.Id is { } id)
            {
                note = await store.Note(id, token) ?? throw new ProgrammeException(404, "Note not found.");
                if (note.EnrollmentId != enrollment.Id) throw new ProgrammeException(404, "Note not found.");
                if (note.Revision != request.Revision) throw new ProgrammeException(409, "This note changed elsewhere. Reload its latest revision before saving your draft.");
                if (note.ModuleId != request.ModuleId || note.SessionId != request.SessionId || note.PromptId != request.PromptId) throw new FluentValidation.ValidationException([new("ModuleId", "A saved note keeps its original attachment and prompt.")]);
            }
            else
            {
                if (request.ModuleId is { } moduleId)
                {
                    var module = modules.SingleOrDefault(x => x.Id == moduleId) ?? throw new ProgrammeException(404, "Module not found.");
                    var completions = await store.Completions(enrollment.Id, token);
                    if (module.Ordinal != Progress.Current(modules, completions) && !Progress.Complete(module, completions)) throw new ProgrammeException(409, "Complete the current module to unlock this module.");
                    if (request.PromptId != null && !module.PreparationPrompts.Any(x => x.Id == request.PromptId)) throw new ProgrammeException(404, "Prompt not found.");
                }
                else if (!bookings.Any(x => x.Id == request.SessionId)) throw new ProgrammeException(404, "Session not found.");
                if (request.PromptId is { } promptId && await store.HasPromptAnswer(enrollment.Id, promptId, token)) throw new ProgrammeException(409, "This prompt already has an answer. Edit the existing note.");
                note = new() { EnrollmentId = enrollment.Id, ModuleId = request.ModuleId, SessionId = request.SessionId, PromptId = request.PromptId, CreatedAt = clock.UtcNow };
                store.Add(note);
            }
            note.Body = request.Body; note.RevisedAt = clock.UtcNow; note.Revision = Guid.NewGuid();
            return ProgrammeReader.Note(note, ProgrammeReader.NoteTitle(note, modules, bookings), true);
        }, ct);
    }
}
