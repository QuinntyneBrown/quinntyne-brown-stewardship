using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Scheduling;

public sealed class GetSessionQueryHandler(IProgrammeStore store, ProgrammeReader reader) : IRequestHandler<GetSessionQuery, BookingResponse>
{
    public async Task<BookingResponse> Handle(GetSessionQuery request, CancellationToken ct)
    {
        var enrollment = await reader.Enrollment(ct);
        var booking = (await store.Bookings(enrollment.Id, ct)).SingleOrDefault(x => x.Id == request.Id) ?? throw new ProgrammeException(404, "Session not found.");
        return reader.Booking(booking, enrollment.Cohort, await store.ProgressModules(enrollment.Cohort.CurriculumKey, ct), await store.Completions(enrollment.Id, ct));
    }
}
