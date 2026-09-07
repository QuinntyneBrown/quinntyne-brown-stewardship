namespace QuinntyneBrownStewardship.Domain.Learning;

// Positions in an authored list are contiguous from one. Every change to a list passes through here so no gap or duplicate appears.
public static class OrdinalSequence
{
    public static int Next<T>(IReadOnlyCollection<T> children) where T : IOrdered => children.Count == 0 ? 1 : children.Max(x => x.Ordinal) + 1;
    // A submitted order must name every child exactly once and nothing else.
    public static bool IsPermutation<T>(IReadOnlyCollection<T> children, IReadOnlyList<Guid> order) where T : IOrdered
        => order.Count == children.Count && order.Distinct().Count() == order.Count && order.All(id => children.Any(x => x.Id == id));
    // Staging moves every child to a position no final position uses, so the unique index never sees two children on one position mid-way.
    public static void Stage<T>(IReadOnlyCollection<T> children) where T : IOrdered { foreach (var child in children) child.Ordinal = -child.Ordinal; }
    public static void Arrange<T>(IReadOnlyCollection<T> children, IReadOnlyList<Guid> order) where T : IOrdered
    {
        for (var index = 0; index < order.Count; index++) children.Single(x => x.Id == order[index]).Ordinal = index + 1;
    }
    // After a removal the remaining children close the gap in their current order.
    public static void Compact<T>(IReadOnlyCollection<T> remaining) where T : IOrdered
    {
        var position = 1;
        foreach (var child in remaining.OrderBy(x => x.Ordinal)) child.Ordinal = position++;
    }
}
