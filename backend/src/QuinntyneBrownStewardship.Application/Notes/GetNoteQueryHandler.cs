using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Notes;

public sealed class GetNoteQueryHandler(IProgrammeStore store, ICurrentParticipant participant) : IRequestHandler<GetNoteQuery, NoteResponse>
{
    public async Task<NoteResponse> Handle(GetNoteQuery request, CancellationToken ct)
    {
        var note = await store.Note(request.Id, ct) ?? throw new ProgrammeException(404, "Note not found.");
        var enrollment = await store.EnrollmentById(note.EnrollmentId, ct) ?? throw new ProgrammeException(404, "Note not found.");
        var owner = enrollment.ParticipantId == participant.Id;
        if (!owner && (enrollment.Cohort.MentorId != participant.Id || !(await store.Participant(participant.Id, ct))!.IsMentor)) throw new ProgrammeException(404, "Note not found.");
        return ProgrammeReader.Note(note, ProgrammeReader.NoteTitle(note, await store.Modules(enrollment.Cohort.CurriculumKey, ct), await store.Bookings(enrollment.Id, ct)), owner);
    }
}
