using Tavstal.BeaconSDK.Models;

namespace Tavstal.BeaconSDK.Services.Abstractions;

/// <summary>
///     Provides HTTP client capabilities for GET-based string retrieval and file downloads, with support for streaming,
///     progress reporting and parallel downloads.
/// </summary>
public interface IHttpService
{
    /// <summary>
    ///     Performs a GET request and returns the response content as a string with progress tracking.
    /// </summary>
    /// <param name="url">The request URL.</param>
    /// <param name="progress">
    ///     Optional progress reporter that receives percentage complete (0-100) as data is received.
    /// </param>
    /// <param name="cancellationToken">Cancellation token observed during the request.</param>
    /// <returns>
    ///     A task that resolves to the response content as a string if successful; otherwise, <see langword="null" />.
    /// </returns>
    Task<string?> GetStringAsync(string url, IProgress<double>? progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads a file from the specified URL and saves it to the target path with progress tracking.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="filePath">The local file path where the downloaded content will be saved.</param>
    /// <param name="sha1">The optional expected SHA-1 hash of the file, used for integrity verification.</param>
    /// <param name="progress">
    ///     Optional progress reporter that receives percentage complete (0-100) as data is downloaded.
    /// </param>
    /// <param name="cancellationToken">Cancellation token observed during the download.</param>
    /// <returns>
    ///     A task that returns <see langword="true"/> if the download succeeds; otherwise, <see langword="false" />.
    /// </returns>
    Task<bool> DownloadFileAsync(string url, string filePath, string? sha1, IProgress<double>? progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads multiple files in parallel with progress tracking for each file.
    /// </summary>
    /// <param name="entries">
    ///     A list of <see cref="DownloadEntry" /> objects representing the files to download, including
    ///     their URLs and target paths.
    /// </param>
    /// <param name="cancellationToken">Cancellation token observed during the downloads.</param>
    /// <returns>A task that completes when all downloads have finished.</returns>
    Task ParallelDownloadFilesAsync(List<DownloadEntry> entries, CancellationToken cancellationToken = default);
}
