using Microsoft.Extensions.Logging;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Tests.Fakes;

/// <summary>
///     A hand-rolled <see cref="IBeaconLogger{T}" /> that captures every log entry
///     so tests can assert on what was written.
/// </summary>
public sealed class CapturingBeaconLogger<T> : IBeaconLogger<T> where T : class
{
    public List<LogEntry> Entries { get; } = new();
    public List<LogLevel> Levels { get; } = new();
    public List<Exception?> Exceptions { get; } = new();

    public void Log(LogLevel logLevel, LogEntry entry, Exception? exception,
        Func<LogEntry, Exception?, string> formatter)
    {
        Levels.Add(logLevel);
        Entries.Add(entry);
        Exceptions.Add(exception);
    }

    public string ModuleName => typeof(T).Name;
    public LogLevel LogLevel { get; set; } = LogLevel.Trace;
    public string? CustomLogFilePath { get; set; }
}
