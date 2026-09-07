using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;

namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class CreateCohortCommandHandler(IProgrammeStore store) : IRequestHandler<CreateCohortCommand, Guid>
{
    public async Task<Guid> Handle(CreateCohortCommand request, CancellationToken ct)
    {
        return await store.Transaction(async token =>
        {
            var mentor = await store.ParticipantByEmail(request.MentorEmail, token);
            if (mentor is not { IsMentor: true }) throw new ProgrammeException(404, "Provision the mentor account first.");
            var curriculum = await store.CurriculumByKey(request.CurriculumKey, token) ?? throw new ProgrammeException(404, "Curriculum not found. Import or create it first.");
            if (!curriculum.IsPublished) throw new ProgrammeException(409, "Publish the curriculum before creating a cohort that follows it.");
            var existing = await store.Cohort(request.Id, token);
            if (existing != null)
            {
                if (existing.StartDate != request.StartDate || existing.MentorId != mentor.Id || existing.CurriculumId != curriculum.Id || existing.TimeZone != request.TimeZone || existing.DurationWeeks != request.DurationWeeks || existing.SessionCadenceWeeks != request.SessionCadenceWeeks)
                    throw new ProgrammeException(409, "That cohort identifier is already configured differently.");
                return existing.Id;
            }
            store.Add(new QuinntyneBrownStewardship.Domain.Enrollment.Cohort { Id = request.Id, StartDate = request.StartDate, MentorId = mentor.Id, MentorName = mentor.DisplayName, CurriculumId = curriculum.Id, TimeZone = request.TimeZone, DurationWeeks = request.DurationWeeks, SessionCadenceWeeks = request.SessionCadenceWeeks });
            return request.Id;
        }, ct);
    }
}
