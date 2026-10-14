using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config.Endpoints;

/// <summary>
///     Represents the Quilt meta and maven endpoint URLs used to download Quilt manifests, profiles and loader jars.
/// </summary>
public class QuiltEndpointConfig
{
    /// <summary>
    ///     Gets or sets the URL of the Quilt version manifest listing available game and loader versions.
    /// </summary>
    [JsonPropertyName("VersionManifestUrl")]
    public string VersionManifestUrl { get; set; } = "https://meta.quiltmc.org/v3/versions";

    /// <summary>
    ///     The URL template for retrieving the Quilt loader JSON profile for a specific Minecraft version and loader version.
    /// </summary>
    /// <remarks>
    ///     Replace `{0}` with the Minecraft version and `{1}` with the Quilt loader version.
    ///     Example: https://meta.quiltmc.org/v3/versions/loader/1.20.4/0.24.0/profile/json
    /// </remarks>
    [JsonPropertyName("LoaderJsonUrl")]
    public string LoaderJsonUrl { get; set; } = "https://meta.quiltmc.org/v3/versions/loader/{0}/{1}/profile/json";

    /// <summary>
    ///     The URL template for downloading the Quilt loader JAR file for a specific version.
    /// </summary>
    /// <remarks>
    ///     Replace `{0}` with the Quilt loader version.
    ///     Example: https://maven.quiltmc.org/repository/release/org/quiltmc/quilt-loader/0.24.0/quilt-loader-0.24.0.jar
    /// </remarks>
    [JsonPropertyName("LoaderJarUrl")]
    public string LoaderJarUrl { get; set; } =
        "https://maven.quiltmc.org/repository/release/org/quiltmc/quilt-loader/{0}/quilt-loader-{0}.jar";
}
