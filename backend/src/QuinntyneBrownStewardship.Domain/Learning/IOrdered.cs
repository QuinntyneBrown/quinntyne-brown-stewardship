namespace QuinntyneBrownStewardship.Domain.Learning;

public interface IOrdered
{
    Guid Id { get; }
    int Ordinal { get; set; }
}
