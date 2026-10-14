using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Java;

/// <summary>
///     Represents a single downloadable Java (JDK) mirror for a specific major version and CPU architecture.
/// </summary>
public class JavaMirror
{
    /// <summary>
    ///     Gets or sets the Java major version the mirror provides (for example <c>8</c>, <c>17</c> or <c>21</c>).
    /// </summary>
    [JsonPropertyName("Version")]
    public required int Version { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the mirror targets an ARM-based architecture.
    ///     When <see langword="false" /> the mirror targets x86_64.
    /// </summary>
    [JsonPropertyName("IsArm")]
    public required bool IsArm { get; set; }

    /// <summary>
    ///     Gets or sets the download URL of the Java distribution archive.
    /// </summary>
    [JsonPropertyName("Url")]
    public required string Url { get; set; }

    /// <summary>
    ///     Gets or sets the optional expected hash of the downloaded archive, used for integrity verification.
    /// </summary>
    [JsonPropertyName("Hash")]
    public string? Hash { get; set; }
}
