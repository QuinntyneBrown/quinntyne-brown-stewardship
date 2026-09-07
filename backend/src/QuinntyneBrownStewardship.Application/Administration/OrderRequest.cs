namespace QuinntyneBrownStewardship.Application.Administration;

// The body of every reorder: the children of one parent, each named once, in the order they should take.
public sealed record OrderRequest(List<Guid> Order);
