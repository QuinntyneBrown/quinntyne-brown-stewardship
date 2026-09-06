using QuinntyneBrownStewardship.Domain.Access;
using QuinntyneBrownStewardship.Domain.Enrollment;
using QuinntyneBrownStewardship.Domain.Learning;
using QuinntyneBrownStewardship.Domain.Scheduling;
using QuinntyneBrownStewardship.Domain.Notes;
using QuinntyneBrownStewardship.Application.Notes;
namespace QuinntyneBrownStewardship.Application.Abstractions;

public interface IProgrammeStore
{
    Task<QuinntyneBrownStewardship.Domain.Enrollment.Enrollment?> Enrollment(Guid participantId, CancellationToken ct);
    Task<QuinntyneBrownStewardship.Domain.Enrollment.Enrollment?> EnrollmentById(Guid id, CancellationToken ct);
    Task<Participant?> Participant(Guid id, CancellationToken ct);
    Task<Participant?> ParticipantByEmail(string email, CancellationToken ct);
    Task<Cohort?> Cohort(Guid id, CancellationToken ct);
    Task<bool> SlotHasBookings(Guid id, CancellationToken ct);
    Task<List<CurriculumModule>> Modules(string curriculumKey, CancellationToken ct);
    Task<List<CurriculumModule>> ProgressModules(string curriculumKey, CancellationToken ct);
    Task<List<SectionCompletion>> Completions(Guid enrollmentId, CancellationToken ct);
    Task<List<AvailabilitySlot>> Slots(Guid mentorId, CancellationToken ct);
    Task<List<Booking>> Bookings(Guid enrollmentId, CancellationToken ct);
    Task<List<Guid>> ClaimedSlots(Guid mentorId, CancellationToken ct);
    Task<List<Note>> NotesPage(Guid enrollmentId, Guid? moduleId, Guid? sessionId, NoteCursor? cursor, bool generalOnly, CancellationToken ct);
    Task<List<Note>> PromptAnswers(Guid enrollmentId, Guid moduleId, CancellationToken ct);
    Task<bool> HasPromptAnswer(Guid enrollmentId, Guid promptId, CancellationToken ct);
    Task<Note?> Note(Guid id, CancellationToken ct);
    void Add<T>(T entity) where T : class;
    Task<T> Transaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);
}

