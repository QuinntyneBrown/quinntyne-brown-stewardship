using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration;

// Every authoring write leaves one record of who changed what and when, inside the same transaction as the change.
public sealed class CurriculumAuditor(ICurriculumStore store, ICurrentParticipant participant, ISystemClock clock, ICorrelationContext correlation)
{
    public void Record(string action, Guid targetId)
        => store.Add(new CurriculumAudit { ActorId = participant.Id, Action = action, TargetId = targetId, CorrelationId = correlation.Id, At = clock.UtcNow });
}
