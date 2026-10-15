namespace Tavstal.BeaconSDK.Models;

/// <summary>
///     Represents a per-resource synchronization entry consisting of a semaphore and a reference count
///     used to track how many callers are currently holding or waiting on the lock.
/// </summary>
public class LockEntry
{
    /// <summary>
    ///     Gets the semaphore used to mutually exclude access to the associated resource.
    /// </summary>
    public SemaphoreSlim Semaphore { get; } = new(1, 1);

    /// <summary>
    ///     The number of active references to this entry, used to determine when it can be released.
    /// </summary>
    public int RefCount;
}
