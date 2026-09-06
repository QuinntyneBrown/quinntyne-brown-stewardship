using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class EnrollParticipantCommandHandler(IProgrammeStore store) : IRequestHandler<EnrollParticipantCommand, Guid>
{
    public async Task<Guid> Handle(EnrollParticipantCommand request, CancellationToken ct)
    {
        return await store.Transaction(async token =>
        {
            var participant = await store.ParticipantByEmail(request.EmailAddress, token) ?? throw new ProgrammeException(404, "Provision the participant first.");
            if (participant.IsMentor) throw new ProgrammeException(409, "A mentor account cannot be enrolled as a participant.");
            var cohort = await store.Cohort(request.CohortId, token) ?? throw new ProgrammeException(404, "Cohort not found.");
            var existing = await store.Enrollment(participant.Id, token);
            if (existing != null)
            {
                if (existing.CohortId != cohort.Id) throw new ProgrammeException(409, "The participant already belongs to another active cohort.");
                return existing.Id;
            }
            var enrollment = new QuinntyneBrownStewardship.Domain.Enrollment.Enrollment { ParticipantId = participant.Id, CohortId = cohort.Id };
            store.Add(enrollment); return enrollment.Id;
        }, ct);
    }
}
