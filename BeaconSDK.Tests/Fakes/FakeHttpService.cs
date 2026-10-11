using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Tests.Fakes;

/// <summary>
///     An <see cref="IHttpService" /> whose <see cref="CreateHttpClient" /> returns a client
///     backed by a <see cref="StubHttpMessageHandler" />. The remaining members delegate to that
///     client where possible and otherwise report "not configured".
/// </summary>
public sealed class FakeHttpService : IHttpService
{
    private readonly HttpClient _client;

    public FakeHttpService(StubHttpMessageHandler handler)
    {
        Handler = handler;
        _client = new HttpClient(handler, false);
    }

    public StubHttpMessageHandler Handler { get; }

    public HttpClient CreateHttpClient(string agent = "BeaconSDK/1.0.0 (+https://github.com/TavstalDev/BeaconSDK)")
    {
        return new HttpClient(Handler, false);
    }

    public async Task<HttpResponseMessage?> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _client.GetAsync(url, cancellationToken);
    }

    public async Task<string?> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _client.GetStringAsync(url, cancellationToken);
    }

    public async Task<string?> GetStringAsync(string url, IProgress<double>? progress,
        CancellationToken cancellationToken = default)
    {
        var result = await GetStringAsync(url, cancellationToken);
        progress?.Report(100);
        return result;
    }

    public async Task<byte[]?> GetByteArrayAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _client.GetByteArrayAsync(url, cancellationToken);
    }

    public async Task<Stream?> GetStreamAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _client.GetStreamAsync(url, cancellationToken);
    }

    public async Task<bool> DownloadFileAsync(string url, string filePath, string? sha1,
        IProgress<double>? progress, CancellationToken cancellationToken = default)
    {
        var bytes = await _client.GetByteArrayAsync(url, cancellationToken);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
        progress?.Report(100);
        return true;
    }

    public async Task ParallelDownloadFilesAsync(List<DownloadEntry> entries,
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
            await DownloadFileAsync(entry.Url, entry.Path, entry.Sha1, entry.Progress, cancellationToken);
    }

    public async Task<HttpResponseMessage?> PostAsync(string url, HttpContent? content,
        CancellationToken cancellationToken = default)
    {
        return await _client.PostAsync(url, content, cancellationToken);
    }
}
