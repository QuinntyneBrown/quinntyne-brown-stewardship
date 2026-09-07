using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public sealed class CreateCurriculumCommandHandler(ICurriculumStore store, CurriculumAuditor auditor, ISystemClock clock) : IRequestHandler<CreateCurriculumCommand, Guid>
{
    public Task<Guid> Handle(CreateCurriculumCommand request, CancellationToken ct) => store.Transaction(async token =>
    {
        // The transaction holds the programme lock, so the check names the conflict and the unique index stays the backstop.
        await KeyRules.Free(store, request.Key, null, token);
        var curriculum = new Curriculum { Key = request.Key, Title = request.Title, CreatedAt = clock.UtcNow };
        store.Add(curriculum);
        auditor.Record("CurriculumCreated", curriculum.Id);
        return curriculum.Id;
    }, ct);
}
