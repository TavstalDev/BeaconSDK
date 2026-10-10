using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;
// ReSharper disable UnusedMethodReturnValue.Global

namespace Tavstal.BeaconSDK.Services.Implementations;

/// <inheritdoc />
public class HttpService : IHttpService
{
    private const int MaxParallelDownloads = 4;
    private readonly HttpClient _httpClient;
    private readonly IBeaconLogger _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HttpService" /> class.
    /// </summary>
    /// <param name="logger">Logger instance used to record diagnostic and error messages for HTTP operations.</param>
    public HttpService(IBeaconLogger<HttpService> logger)
    {
        _logger = logger;
        _httpClient = CreateHttpClient();
    }

    /// <inheritdoc />
    public HttpClient CreateHttpClient(string agent = "BeaconSDK/1.0.0 (+https://github.com/TavstalDev/BeaconSDK)")
    {
        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions { EnabledSslProtocols = SslProtocols.Tls12 },
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true
        };
        handler.EnableMultipleHttp2Connections = false;
        var client = new HttpClient(handler);

        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(agent);
        client.Timeout = TimeSpan.FromSeconds(120);
        return client;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage?> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP request to {url} failed with {ex.StatusCode}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to make GET request to {url}:");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetStringAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP request to {url} failed with {ex.StatusCode}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to make GET request to {url}:");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetStringAsync(string url, IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var memoryStream = new MemoryStream();
            var buffer = new byte[8192];
            int bytesRead;
            long totalBytesRead = 0;

            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await memoryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesRead += bytesRead;

                if (progress != null && contentLength.HasValue)
                {
                    var percentage = (double)totalBytesRead / contentLength.Value * 100;
                    progress.Report(percentage);
                }
            }

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP request to {url} failed with {ex.StatusCode}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to make GET request to {url}:");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetByteArrayAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP request to {url} failed with {ex.StatusCode}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to make GET request to {url}:");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Stream?> GetStreamAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetStreamAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP request to {url} failed with {ex.StatusCode}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to make GET request to {url}:");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DownloadFileAsync(string url, string filePath, string? sha1, IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        string tempFilePath = filePath + ".tmp";
        try
        {
            if (File.Exists(filePath))
            {
                bool shouldReturn = true;
                if (!string.IsNullOrEmpty(sha1))
                {
                    string? hash = await FileSystemHelper.GetFileHashAsync(filePath);
                    if (!string.Equals(sha1, hash, StringComparison.InvariantCultureIgnoreCase) &&
                        !string.IsNullOrEmpty(hash))
                    {
                        shouldReturn =  false;
                        FileSystemHelper.DeleteFile(filePath);
                    }
                }
                if (shouldReturn)
                    return true;
            }

            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var contentLength = response.Content.Headers.ContentLength;

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await using var fileStream =
                new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 8192);

            var buffer = new byte[8192];
            int bytesRead;
            long totalBytesRead = 0;

            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesRead += bytesRead;

                if (progress != null && contentLength.HasValue)
                {
                    var percentage = (double)totalBytesRead / contentLength.Value * 100;
                    progress.Report(percentage);
                }
            }

            fileStream.Close();
            if (!string.IsNullOrEmpty(sha1))
            {
                string? hash = await FileSystemHelper.GetFileHashAsync(tempFilePath);
                if (!string.Equals(sha1, hash, StringComparison.InvariantCultureIgnoreCase) &&
                    !string.IsNullOrEmpty(hash))
                {
                    _logger.LogWarning($"{url}: Had mismatching sha1 checksum, skipping download...");
                    return false;
                }
            }

            File.Move(tempFilePath, filePath, true);

            return true; // Return the path to the downloaded file
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(tempFilePath))
                FileSystemHelper.DeleteFile(tempFilePath);
            throw;
        }
        catch (HttpRequestException ex)
        {
            if (File.Exists(tempFilePath))
                FileSystemHelper.DeleteFile(tempFilePath);
            _logger.LogError($"HTTP request to {url} failed with {ex.StatusCode}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            if (File.Exists(tempFilePath))
                FileSystemHelper.DeleteFile(tempFilePath);
            _logger.LogCritical(ex, $"Failed to download file from {url}:");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task ParallelDownloadFilesAsync(List<DownloadEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(MaxParallelDownloads);
        var downloadTasks = new List<Task>();

        foreach (var entry in entries)
        {
            await semaphore.WaitAsync(cancellationToken);
            var task = Task.Run(async () =>
            {
                try
                {
                    await DownloadFileAsync(entry.Url, entry.Path, entry.Sha1, entry.Progress, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);
            downloadTasks.Add(task);
        }

        await Task.WhenAll(downloadTasks);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage?> PostAsync(string url, HttpContent? content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.PostAsync(url, content, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP request to {url} failed with {ex.StatusCode}: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to make POST request to {url}:");
            return null;
        }
    }
}
