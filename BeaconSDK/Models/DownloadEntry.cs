namespace Tavstal.BeaconSDK.Models;

/// <summary>
///     Represents a file download entry with a source URL, destination path, an optional SHA-1 hash for integrity
///     verification, and optional progress reporting.
/// </summary>
public class DownloadEntry
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DownloadEntry" /> class.
    /// </summary>
    public DownloadEntry()
    {
        Url = string.Empty;
        Path = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DownloadEntry" /> class with a URL, path, optional SHA-1 hash,
    ///     and optional progress reporter.
    /// </summary>
    /// <param name="url">The source URL of the file to download.</param>
    /// <param name="path">The local file path to save the download to.</param>
    /// <param name="sha1">The optional expected SHA-1 hash of the file, used for integrity verification.</param>
    /// <param name="_progressReporter">An optional progress reporter that reports download progress and status updates.</param>
    public DownloadEntry(string url, string path, string? sha1 = null, IProgressReporter? _progressReporter = null)
    {
        Url = url;
        Path = path;
        Sha1 = sha1;
        if (_progressReporter != null)
        {
            var fileName = System.IO.Path.GetFileName(path);
            Progress = new Progress<double>(p =>
            {
                _progressReporter.Report(p, fileName);
            });
        }
    }

    /// <summary>
    ///     Gets or sets the source URL of the file to download.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    ///     Gets or sets the local file path where the downloaded file will be saved.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    ///     Gets or sets the optional expected SHA-1 hash of the file, used to verify the file's integrity.
    /// </summary>
    public string? Sha1 { get; set; }

    /// <summary>
    ///     Gets or sets the progress reporter for the download operation.
    /// </summary>
    public IProgress<double>? Progress { get; set; }
}
