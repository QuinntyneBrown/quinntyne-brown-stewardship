using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration;

// One rule for every ordered list: the submitted order must be a permutation, positions are staged clear of the unique
// index, flushed, and then assigned; the transaction's final save is the second pass, so a failure rolls back both.
public static class Reordering
{
    public static async Task Apply<T>(ICurriculumStore store, IReadOnlyCollection<T> children, IReadOnlyList<Guid> order, string noun, string parent, CancellationToken ct) where T : IOrdered
    {
        if (!OrdinalSequence.IsPermutation(children, order)) throw new ProgrammeException(409, $"The submitted order does not match the {noun} of this {parent}. Reload and try again.");
        OrdinalSequence.Stage(children);
        await store.Flush(ct);
        OrdinalSequence.Arrange(children, order);
    }
}
