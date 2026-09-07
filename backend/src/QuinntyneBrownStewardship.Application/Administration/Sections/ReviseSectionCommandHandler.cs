using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Modules;
using QuinntyneBrownStewardship.Application.Common;
namespace QuinntyneBrownStewardship.Application.Administration.Sections;

// A revision reaches participants on their next read and needs no publication; completion stays recorded against the section, not its wording.
public sealed class ReviseSectionCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<ReviseSectionCommand, RevisionResponse>
{
    public Task<RevisionResponse> Handle(ReviseSectionCommand request, CancellationToken ct) => store.Transaction(async token =>
    {
        var section = await store.RequiredSection(request.Id, token);
        if (section.Revision != request.Revision) throw new ProgrammeException(409, "This section changed since it was opened. Reload it to see the current content.");
        section.Title = request.Title; section.Reading = request.Reading; section.Revision = Guid.NewGuid();
        auditor.Record("SectionRevised", section.Id);
        return new RevisionResponse(section.Revision);
    }, ct);
}
