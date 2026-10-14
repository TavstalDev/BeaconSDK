using System.Text.Json.Serialization;
using Tavstal.BeaconSDK.Models.Config.Endpoints;

namespace Tavstal.BeaconSDK.Models.Config;

/// <summary>
///     Groups the remote endpoint URLs used by the Beacon SDK for authentication, manifests and mod loader downloads.
/// </summary>
public class EndpointConfig
{
    /// <summary>
    ///     Gets or sets the Microsoft identity and Xbox authentication endpoint URLs.
    /// </summary>
    [JsonPropertyName("Microsoft")]
    public MicrosoftEndpointConfig Microsoft { get; set; } = new();

    /// <summary>
    ///     Gets or sets the Mojang/Minecraft Services and launcher metadata endpoint URLs.
    /// </summary>
    [JsonPropertyName("Mojang")]
    public MojangEndpointConfig Mojang { get; set; } = new();

    /// <summary>
    ///     Gets or sets the Fabric meta and maven endpoint URLs.
    /// </summary>
    [JsonPropertyName("Fabric")]
    public FabricEndpointConfig Fabric { get; set; } = new();

    /// <summary>
    ///     Gets or sets the Quilt meta and maven endpoint URLs.
    /// </summary>
    [JsonPropertyName("Quilt")]
    public QuiltEndpointConfig Quilt { get; set; } = new();

    /// <summary>
    ///     Gets or sets the Forge maven and files endpoint URLs.
    /// </summary>
    [JsonPropertyName("Forge")]
    public ForgeEndpointConfig Forge { get; set; } = new();

    /// <summary>
    ///     Gets or sets the NeoForge maven endpoint URLs.
    /// </summary>
    [JsonPropertyName("NeoForge")]
    public NeoForgeEndpointConfig NeoForge { get; set; } = new();
}
