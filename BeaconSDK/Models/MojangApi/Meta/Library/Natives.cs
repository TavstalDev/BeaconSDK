using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;

/// <summary>
///     Represents the native configurations for different operating systems.
/// </summary>
public class Natives
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Natives" /> class.
    /// </summary>
    public Natives()
    {
        Windows = string.Empty;
        Osx = string.Empty;
        Linux = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Natives" /> class with specified configurations for Windows, macOS,
    ///     and Linux.
    /// </summary>
    /// <param name="windows">The native configuration for Windows.</param>
    /// <param name="osx">The native configuration for macOS.</param>
    /// <param name="linux">The native configuration for Linux.</param>
    public Natives(string windows, string osx, string linux)
    {
        Windows = windows;
        Osx = osx;
        Linux = linux;
    }

    /// <summary>
    ///     Gets or sets the native configuration for Windows.
    /// </summary>
    [JsonPropertyName("windows")]
    public string Windows { get; set; }

    /// <summary>
    ///     Gets or sets the native configuration for macOS.
    /// </summary>
    [JsonPropertyName("osx")]
    public string Osx { get; set; }

    /// <summary>
    ///     Gets or sets the native configuration for Linux.
    /// </summary>
    [JsonPropertyName("linux")]
    public string Linux { get; set; }
}
