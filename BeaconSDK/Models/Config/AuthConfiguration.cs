using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config;

/// <summary>
///     Represents the authentication settings for the Beacon SDK, including the Microsoft OAuth2 client ID
///     and device code flow behavior.
/// </summary>
public class AuthConfiguration
{
    /// <summary>
    ///     Gets or sets the Microsoft OAuth2 client ID used for authentication requests.
    ///     The client ID comes from the Microsoft Azure application registration.
    /// </summary>
    [JsonPropertyName("MicrosoftClientId")]
    public required string? MicrosoftClientId { get; set; }

    /// <summary>
    ///     Gets or sets the interval at which the device code endpoint is polled while waiting for
    ///     the user to complete device code authentication. Defaults to 5 seconds.
    /// </summary>
    [JsonPropertyName("DeviceCodePollInterval")]
    public TimeSpan DeviceCodePollInterval { get; set; } = TimeSpan.FromSeconds(5);
}
