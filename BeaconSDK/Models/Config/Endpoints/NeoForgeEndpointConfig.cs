using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config.Endpoints;

/// <summary>
///     Represents the NeoForge maven endpoint URLs used to download the NeoForge version manifest,
///     universal jars and installer jars.
/// </summary>
public class NeoForgeEndpointConfig
{
    /// <summary>
    ///     The URL of the NeoForge maven metadata XML listing all published NeoForge versions.
    /// </summary>
    [JsonPropertyName("VersionManifest")]
    public string VersionManifest { get; set; } =
        "https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml";

    /// <summary>
    ///     The URL template for downloading the NeoForge universal JAR file for a specific version.
    /// </summary>
    /// <remarks>
    ///     Replace `{0}` with the NeoForge version (e.g., "20.4.237").
    ///     Example:
    ///     https://maven.neoforged.net/releases/net/neoforged/neoforge/20.4.237/neoforge-20.4.237-universal.jar
    /// </remarks>
    [JsonPropertyName("LoaderUniversalZipUrl")]
    public string LoaderUniversalZipUrl { get; set; } =
        "https://maven.neoforged.net/releases/net/neoforged/neoforge/{0}/neoforge-{0}-universal.jar";

    /// <summary>
    ///     The URL template for downloading the NeoForge installer JAR file for a specific version.
    /// </summary>
    /// <remarks>
    ///     Replace `{0}` with the NeoForge version (e.g., "20.4.237").
    ///     Example:
    ///     https://maven.neoforged.net/releases/net/neoforged/neoforge/20.4.237/neoforge-20.4.237-installer.jar
    /// </remarks>
    [JsonPropertyName("InstallerJarUrl")]
    public string InstallerJarUrl { get; set; } =
        "https://maven.neoforged.net/releases/net/neoforged/neoforge/{0}/neoforge-{0}-installer.jar";
}
