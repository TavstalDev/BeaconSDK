using System.Collections.Concurrent;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Tests.Fakes;

/// <summary>
///     An <see cref="ILogWriter" /> that records every enqueued entry instead of touching the filesystem.
/// </summary>
public sealed class CapturingLogWriter : ILogWriter
{
    public ConcurrentQueue<(string Entry, string? Path)> Entries { get; } = new();

    public void EnqueueLog(string entry, string? logFilePath = null)
    {
        Entries.Enqueue((entry, logFilePath));
    }
}