using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class CapturedLogs : ILoggerProvider
{
    public ConcurrentQueue<string> Messages { get; } = new();
    public ILogger CreateLogger(string categoryName) => new CapturedLogger(Messages);
    public void Dispose() { }
}
