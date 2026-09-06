using QuinntyneBrownStewardship.Application.Abstractions;
namespace QuinntyneBrownStewardship.Infrastructure.Access;

public sealed class SystemClock : ISystemClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
