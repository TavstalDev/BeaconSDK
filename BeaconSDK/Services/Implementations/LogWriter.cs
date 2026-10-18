using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Services.Implementations;

/// <inheritdoc cref="ILogWriter" />
public class LogWriter : IHostedService, ILogWriter
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _queues = new();
    private readonly ConcurrentDictionary<string, LockEntry> _fileLocks = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly BeaconConfiguration _configuration;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LogWriter" /> class.
    /// </summary>
    /// <param name="configuration">
    ///     The Beacon SDK configuration. Its <see cref="BeaconConfiguration.Logging" /> supplies the default log level and
    ///     fallback file path used for entries that are enqueued without an explicit path.
    /// </param>
    public LogWriter(BeaconConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cts)
    {
        try
        {
            Task.Run(async () => await ProcessLogQueueAsync(cts), cts);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _signal.Dispose();
        _queues.Clear();
        _fileLocks.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void EnqueueLog(string entry, string? logFilePath = null)
    {
        if (string.IsNullOrEmpty(logFilePath) && string.IsNullOrEmpty(_configuration.Logging.DefaultLogFilePath))
            return;

        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {entry}";
        var queue = _queues.GetOrAdd(logFilePath ?? _configuration.Logging.DefaultLogFilePath!, _ => new ConcurrentQueue<string>());
        queue.Enqueue(logEntry);
        _signal.Release();
    }

    private async Task ProcessLogQueueAsync(CancellationToken cts)
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                await _signal.WaitAsync(cts);

                foreach (var (path, queue) in _queues)
                {
                    if (!queue.TryDequeue(out var line))
                        continue;

                    var fileLock = _fileLocks.GetOrAdd(path, _ => new LockEntry());
                    fileLock.RefCount++;
                    await fileLock.Semaphore.WaitAsync(cts);
                    try
                    {
                        string directory = Path.GetDirectoryName(path) ?? string.Empty;
                        if (!string.IsNullOrEmpty(directory))
                            Directory.CreateDirectory(directory);
                        await File.AppendAllTextAsync(path, line + Environment.NewLine, cts);
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
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
    }
}
