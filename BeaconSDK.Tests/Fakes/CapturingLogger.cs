using Microsoft.Extensions.Logging;

namespace Tavstal.BeaconSDK.Tests.Fakes;

/// <summary>
///     An <see cref="ILogger{TCategoryName}" /> that records every call so tests can assert on what the SDK forwarded
///     to <c>Microsoft.Extensions.Logging</c>.
/// </summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception), exception));
    }
}