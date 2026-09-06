using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace QuinntyneBrownStewardship.Api.Tests;

public sealed class CapturedLogger(ConcurrentQueue<string> messages) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel)) messages.Enqueue(formatter(state, exception));
    }
}
