using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config.Endpoints;

/// <summary>
///     Represents the Forge maven endpoint URLs used to download the Forge version manifest, universal jars and installer jars.
/// </summary>
public class ForgeEndpointConfig
{
    /// <summary>
    ///     The URL of the Forge maven metadata XML listing all published Forge versions.
    /// </summary>
    [JsonPropertyName("VersionManifest")]
    public string VersionManifest { get; set; } =
        "https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml";

    /// <summary>
    ///     The URL template for downloading the Forge universal ZIP file for a specific version.
    /// </summary>
    /// <remarks>
    ///     Replace `{0}` with the full Forge version (e.g., "1.2.5-7.8.0.735") and `{1}` with the artifact
    ///     classifier (e.g., "universal" or "client").
    ///     Example:
    ///     https://files.minecraftforge.net/maven/net/minecraftforge/forge/1.2.5-7.8.0.735/forge-1.2.5-7.8.0.735-universal.zip
    /// </remarks>
    [JsonPropertyName("LoaderUniversalZipUrl")]
    public string LoaderUniversalZipUrl { get; set; } =
        "https://files.minecraftforge.net/maven/net/minecraftforge/forge/{0}/forge-{0}-{1}.zip";

    /// <summary>
    ///     The URL template for downloading the Forge installer JAR file for a specific version.
    /// </summary>
    /// <remarks>
    ///     Replace `{0}` with the full Forge version (e.g., "1.20.4-49.0.38").
    ///     Example:
    ///     https://files.minecraftforge.net/maven/net/minecraftforge/forge/1.20.4-49.0.38/forge-1.20.4-49.0.38-installer.jar
    /// </remarks>
    [JsonPropertyName("InstallerJarUrl")]
    public string InstallerJarUrl { get; set; } =
        "https://files.minecraftforge.net/maven/net/minecraftforge/forge/{0}/forge-{0}-installer.jar";
}
