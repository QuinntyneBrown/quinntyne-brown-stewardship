using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Application.Enrollment;

public sealed class GetEnrollmentQueryHandler(IAccessStore store, ICurrentParticipant participant, ISystemClock clock) : IRequestHandler<GetEnrollmentQuery, EnrollmentResponse>
{
    public async Task<EnrollmentResponse> Handle(GetEnrollmentQuery request, CancellationToken cancellationToken)
    {
        var enrollment = await store.FindEnrollment(participant.Id, cancellationToken);
        if (enrollment == null) return new(false);
        var cohort = enrollment.Cohort;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(cohort.TimeZone)).DateTime);
        return new(true, cohort.Id, cohort.MentorName, cohort.StartDate, cohort.EndDate, cohort.CurrentWeek(today), cohort.SessionAllowance, cohort.HasEnded(today), cohort.DurationWeeks, cohort.SessionCadenceWeeks, cohort.Curriculum.IsPublished);
    }
}
