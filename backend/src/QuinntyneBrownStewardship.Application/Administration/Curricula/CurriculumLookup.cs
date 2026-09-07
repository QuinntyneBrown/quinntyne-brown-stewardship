using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

public static class CurriculumLookup
{
    // The programme a write addresses, or the 404 every authoring route answers for an unknown identifier.
    public static async Task<Curriculum> Required(this ICurriculumStore store, Guid id, CancellationToken ct)
        => await store.Curriculum(id, ct) ?? throw new ProgrammeException(404, "Programme not found.");
}
