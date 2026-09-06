using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class TestClock : ISystemClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
