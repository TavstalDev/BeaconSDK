using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models;

/// <summary>
///     Describes a remote file that the SDK downloads at install time, such as the launch wrapper
///     or a legacy Forge support library.
/// </summary>
public class FileEntry
{
    /// <summary>
    ///     Gets or sets the URL the file is downloaded from. An empty value disables the download.
    /// </summary>
    [JsonPropertyName("Url")]
    public required string Url { get; set; }

    /// <summary>
    ///     Gets or sets the expected SHA1 hash of the file used to verify an already existing local copy.
    ///     When null or empty, the hash check is skipped.
    /// </summary>
    [JsonPropertyName("Sha1")]
    public string? Sha1 { get; set; }
}
