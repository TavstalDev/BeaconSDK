namespace Tavstal.BeaconSDK.Services.Abstractions;

/// <summary>
///     Persists log entries to disk without blocking the caller that produced them.
/// </summary>
/// <remarks>
///     Implementations buffer entries in memory and drain them on a background loop, so <see cref="EnqueueLog" />
///     returns immediately. File writes are serialized per target path, and parent directories are created on demand.
/// </remarks>
public interface ILogWriter
{
    /// <summary>
    ///     Enqueues a log entry for asynchronous file writing. Entries with no resolvable target path are discarded.
    /// </summary>
    /// <param name="entry">The log message to enqueue. Implementations may prefix it with a timestamp.</param>
    /// <param name="logFilePath">
    ///     Optional target file path. When null or empty, the implementation's configured default path is used.
    /// </param>
    void EnqueueLog(string entry, string? logFilePath = null);
}