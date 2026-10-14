using System.Net;
using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config;

/// <summary>
///     Represents the HTTP client settings used by <see cref="Tavstal.BeaconSDK.Services.Implementations.HttpService" />
///     and the parallel download limits used by the download services.
/// </summary>
public class HttpConfig
{
    /// <summary>
    ///     Gets or sets the value of the <c>User-Agent</c> header sent with every request.
    /// </summary>
    [JsonPropertyName("Agent")]
    public string Agent { get; set; } = "BeaconSDK/1.0.0 (+https://github.com/TavstalDev/BeaconSDK)";

    /// <summary>
    ///     Gets or sets a value indicating whether TLS 1.2 is enforced on HTTP connections.
    ///     When <see langword="false" />, the system default SSL protocols are used. Defaults to <see langword="true" />.
    /// </summary>
    [JsonPropertyName("EnableSslProtocols")]
    public bool EnableSslProtocols { get; set; } = true;

    /// <summary>
    ///     Gets or sets the decompression methods enabled on the HTTP handler. Defaults to GZip and Deflate.
    /// </summary>
    [JsonPropertyName("AutomaticDecompression")]
    public DecompressionMethods AutomaticDecompression { get; set; } = DecompressionMethods.GZip | DecompressionMethods.Deflate;

    /// <summary>
    ///     Gets or sets a value indicating whether HTTP redirects are followed automatically. Defaults to <see langword="true" />.
    /// </summary>
    [JsonPropertyName("AllowAutoRedirect")]
    public bool AllowAutoRedirect { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether multiple HTTP/2 connections are allowed per host.
    ///     Defaults to <see langword="false" />.
    /// </summary>
    [JsonPropertyName("EnableMultipleHttp2Connections")]
    public bool EnableMultipleHttp2Connections { get; set; } = false;

    /// <summary>
    ///     Gets or sets the value of the <c>Accept</c> header sent with every request. Defaults to <c>application/json</c>.
    /// </summary>
    [JsonPropertyName("AcceptHeader")]
    public string AcceptHeader { get; set; } = "application/json";

    /// <summary>
    ///     Gets or sets the request timeout applied to created <see cref="HttpClient" /> instances. Defaults to 120 seconds.
    /// </summary>
    [JsonPropertyName("Timeout")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>
    ///     Gets or sets the maximum number of files downloaded in parallel. Defaults to 4.
    /// </summary>
    [JsonPropertyName("MaxParallelDownloads")]
    public byte MaxParallelDownloads { get; set; } = 4;

    /// <summary>
    ///     Gets or sets the size in bytes of the read buffers and file streams used when downloading files. Defaults to 8192.
    /// </summary>
    [JsonPropertyName("BufferSize")]
    public int BufferSize { get; set; } = 8192;

    /// <summary>
    ///     Gets or sets the suffix appended to a target file name while it is being downloaded,
    ///     before it is moved to its final path. Defaults to <c>.tmp</c>.
    /// </summary>
    [JsonPropertyName("TempFileSuffix")]
    public string TempFileSuffix { get; set; } = ".tmp";
}
