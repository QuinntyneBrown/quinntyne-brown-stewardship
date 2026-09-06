using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class CreateCohortCommandHandler(IProgrammeStore store) : IRequestHandler<CreateCohortCommand, Guid>
{
    public async Task<Guid> Handle(CreateCohortCommand request, CancellationToken ct)
    {
        return await store.Transaction(async token =>
        {
            var mentor = await store.ParticipantByEmail(request.MentorEmail, token);
            if (mentor is not { IsMentor: true }) throw new ProgrammeException(404, "Provision the mentor account first.");
            if ((await store.Modules(request.CurriculumKey, token)).Count != 12) throw new ProgrammeException(409, "Import the twelve-module curriculum first.");
            var existing = await store.Cohort(request.Id, token);
            if (existing != null)
            {
                if (existing.StartDate != request.StartDate || existing.MentorId != mentor.Id || existing.CurriculumKey != request.CurriculumKey || existing.TimeZone != request.TimeZone) throw new ProgrammeException(409, "That cohort identifier is already configured differently.");
                return existing.Id;
            }
            store.Add(new QuinntyneBrownStewardship.Domain.Enrollment.Cohort { Id = request.Id, StartDate = request.StartDate, MentorId = mentor.Id, MentorName = mentor.DisplayName, CurriculumKey = request.CurriculumKey, TimeZone = request.TimeZone });
            return request.Id;
        }, ct);
    }
}
