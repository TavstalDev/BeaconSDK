using System.Text.Json.Serialization;
using Tavstal.BeaconSDK.Enums;

namespace Tavstal.BeaconSDK.Models.ModLoaders.Quilt;

/// <summary>
///     Represents the manifest for a Quilt mod loader, containing details about the game version
///     and the mod loader version.
/// </summary>
public class QuiltManifest : IModManifest
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="QuiltManifest" /> class with default values.
    /// </summary>
    public QuiltManifest()
    {
        GameVersion = string.Empty;
        Version = string.Empty;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="QuiltManifest" /> class with a specified version.
    /// </summary>
    /// <param name="version">The version of the Quilt mod loader.</param>
    public QuiltManifest(string version)
    {
        GameVersion = string.Empty;
        Version = version;
    }

    /// <inheritdoc />
    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("version")]
    public string Version { get; set; }

    /// <inheritdoc />
    [JsonIgnore]
    public EMinecraftKind LoaderKind { get; } = EMinecraftKind.QUILT;

    /// <inheritdoc />
    public bool EqualsGameVersion(string gameVersion)
    {
        return true;
        // Quilt loader versions are generally compatible across multiple game versions.
    }
}
