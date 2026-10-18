using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config;

/// <summary>
///     Represents the root configuration for the Beacon SDK.
/// </summary>
public class BeaconConfiguration
{
    /// <summary>
    ///     Gets or sets the authentication settings, such as the Microsoft OAuth2 client ID.
    /// </summary>
    [JsonPropertyName("Auth")]
    public required AuthConfiguration Auth { get; set; }

    /// <summary>
    ///     Gets or sets the data protection settings used to persist encryption keys and platform fallback keys.
    /// </summary>
    [JsonPropertyName("DataProtection")]
    public required DataProtectionConfig DataProtection { get; set; }

    /// <summary>
    ///     Gets or sets the HTTP client settings, such as user agent, timeout and download parallelism.
    /// </summary>
    [JsonPropertyName("Http")]
    public HttpConfig Http { get; set; } = new();

    /// <summary>
    ///     Gets or sets the remote endpoint URLs used for authentication, manifests and mod loader downloads.
    /// </summary>
    [JsonPropertyName("Endpoints")]
    public EndpointConfig Endpoints { get; set; } = new();

    /// <summary>
    ///     Gets or sets the logging settings used by the loggers the SDK registers in the container.
    /// </summary>
    [JsonPropertyName("Logging")]
    public LoggingConfig Logging { get; set; } = new();
}
