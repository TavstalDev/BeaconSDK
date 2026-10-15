using System.Collections.Concurrent;
using Tavstal.BeaconSDK.Models;

namespace Tavstal.BeaconSDK.Helpers;

/// <summary>
///     Provides a static, thread-safe log buffering and file writing pipeline.
/// </summary>
public static class LoggerHelper
{
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _queues = new();
    private static readonly ConcurrentDictionary<string, LockEntry> _fileLocks = new();
    private static readonly SemaphoreSlim _signal = new(0);

    /// <summary>
    ///     Gets or sets the file path used by <see cref="EnqueueLog" /> when no explicit path is supplied.
    ///     While this is empty, log entries without a path are discarded.
    /// </summary>
    public static string DefaultLogFilePath { get; set; } = string.Empty;

    /// <summary>
    ///     Enqueues a log entry for asynchronous file writing.
    /// </summary>
    /// <param name="entry">The raw log message (without timestamp) to enqueue.</param>
    /// <param name="logFilePath">
    ///     Optional target file path. If <see langword="null" />, <see cref="DefaultLogFilePath" /> is
    ///     used.
    /// </param>
    public static void EnqueueLog(string entry, string? logFilePath = null)
    {
        if (string.IsNullOrEmpty(logFilePath) && string.IsNullOrEmpty(DefaultLogFilePath))
            return;

        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {entry}";
        var queue = _queues.GetOrAdd(logFilePath ?? DefaultLogFilePath, _ => new ConcurrentQueue<string>());
        queue.Enqueue(logEntry);
        _signal.Release();
    }

    /// <summary>
    ///     Continuously processes queued log entries and appends them to their target files.
    /// </summary>
    /// <param name="token">Cancellation token used to stop the processing loop.</param>
    /// <returns>A task representing the background writer loop.</returns>
    public static async Task ProcessLogQueueAsync(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            await _signal.WaitAsync(token);

            foreach (var (path, queue) in _queues)
            {
                if (!queue.TryDequeue(out var line))
                    continue;

                var fileLock = _fileLocks.GetOrAdd(path, _ => new LockEntry());
                fileLock.RefCount++;
                await fileLock.Semaphore.WaitAsync(token);
                try
                {
                    await File.AppendAllTextAsync(path, line + Environment.NewLine, token);
                }
                finally
                {
                    fileLock.Semaphore.Release();
                    fileLock.RefCount--;
                    if (fileLock.RefCount == 0)
                        _fileLocks.TryRemove(path, out _);
                }
            }
        }
    }
}
