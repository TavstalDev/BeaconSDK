using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config.Endpoints;

/// <summary>
///     Represents the Fabric meta and maven endpoint URLs used to download Fabric manifests, profiles and loader jars.
/// </summary>
public class FabricEndpointConfig
{
    /// <summary>
    ///     Gets or sets the URL of the Fabric version manifest listing available game, loader and interop versions.
    /// </summary>
    [JsonPropertyName("VersionManifestUrl")]
    public string VersionManifestUrl { get; set; } = "https://meta.fabricmc.net/v2/versions";

    /// <summary>
    ///     The URL template for retrieving the Fabric loader JSON profile for a specific Minecraft version and loader version.
    /// </summary>
    /// <remarks>
    ///     Replace `{0}` with the Minecraft version and `{1}` with the Fabric loader version.
    ///     Example: https://meta.fabricmc.net/v2/versions/loader/1.16.5/0.15.6/profile/json
    /// </remarks>
    [JsonPropertyName("LoaderJsonUrl")]
    public string LoaderJsonUrl { get; set; } = "https://meta.fabricmc.net/v2/versions/loader/{0}/{1}/profile/json";

    /// <summary>
    ///     The URL template for downloading the Fabric loader JAR file for a specific version.
    /// </summary>
    /// <remarks>
    ///     Replace `{0}` with the Fabric loader version.
    ///     Example: https://maven.fabricmc.net/net/fabricmc/fabric-loader/0.15.6/fabric-loader-0.15.6.jar
    /// </remarks>
    [JsonPropertyName("LoaderJarUrl")]
    public string LoaderJarUrl { get; set; } =
        "https://maven.fabricmc.net/net/fabricmc/fabric-loader/{0}/fabric-loader-{0}.jar";
}
