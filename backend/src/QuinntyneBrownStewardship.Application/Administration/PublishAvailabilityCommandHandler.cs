using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Administration;

public sealed class PublishAvailabilityCommandHandler(IProgrammeStore store, ISystemClock clock) : IRequestHandler<PublishAvailabilityCommand, int>
{
    public async Task<int> Handle(PublishAvailabilityCommand request, CancellationToken ct)
    {
        return await store.Transaction(async token =>
        {
            var mentor = await store.ParticipantByEmail(request.Document.MentorEmail, token);
            if (mentor is not { IsMentor: true }) throw new ProgrammeException(404, "Mentor not found.");
            var slots = await store.Slots(mentor.Id, token);
            foreach (var source in request.Document.Slots)
            {
                var slot = slots.SingleOrDefault(x => x.Id == source.Id);
                if (slot != null && slot.StartsAt == source.StartsAt && slot.DurationMinutes == source.DurationMinutes) continue;
                if (source.StartsAt <= clock.UtcNow) throw new ProgrammeException(409, "Publish future availability only.");
                if (slot != null && await store.SlotHasBookings(slot.Id, token)) throw new ProgrammeException(409, "A slot referenced by a booking cannot be changed.");
                if (slot == null) { slot = new() { Id = source.Id, MentorId = mentor.Id }; store.Add(slot); slots.Add(slot); }
                slot.StartsAt = source.StartsAt.ToUniversalTime(); slot.DurationMinutes = source.DurationMinutes;
            }
            var ordered = slots.OrderBy(x => x.StartsAt).ToList();
            for (var i = 1; i < ordered.Count; i++) if (ordered[i - 1].StartsAt.AddMinutes(ordered[i - 1].DurationMinutes) > ordered[i].StartsAt) throw new ProgrammeException(409, "Mentor slots cannot overlap.");
            return request.Document.Slots.Count;
        }, ct);
    }
}
