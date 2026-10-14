using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config.Endpoints;

/// <summary>
///     Represents the Mojang and Minecraft Services endpoint URLs used for authentication,
///     profile lookups, asset downloads and manifest downloads.
/// </summary>
public class MojangEndpointConfig
{
    /// <summary>
    ///     Gets or sets the Minecraft Services endpoint that exchanges an Xbox Live token for a Minecraft access token.
    /// </summary>
    [JsonPropertyName("AuthUrl")]
    public string AuthUrl { get; set; } = "https://api.minecraftservices.com/authentication/login_with_xbox";

    /// <summary>
    ///     Gets or sets the endpoint returning the authenticated player's Minecraft profile.
    /// </summary>
    [JsonPropertyName("ProfileUrl")]
    public string ProfileUrl { get; set; } = "https://api.minecraftservices.com/minecraft/profile";

    /// <summary>
    ///     Gets or sets the endpoint returning the game ownership (entitlement) data used to verify Minecraft access.
    /// </summary>
    [JsonPropertyName("OwnershipUrl")]
    public string OwnershipUrl { get; set; } = "https://api.minecraftservices.com/entitlements/mcstore";

    /// <summary>
    ///     Gets or sets the URL of the vanilla Minecraft version manifest used to resolve game versions.
    /// </summary>
    [JsonPropertyName("MinecraftManifestUrl")]
    public string MinecraftManifestUrl { get; set; } = "https://launchermeta.mojang.com/mc/game/version_manifest.json";

    /// <summary>
    ///     Gets or sets the base URL of the asset object store. Asset objects are fetched from
    ///     <c>{MinecraftResourcesUrl}/{first two hash characters}/{hash}</c>.
    /// </summary>
    [JsonPropertyName("MinecraftResourcesUrl")]
    public string MinecraftResourcesUrl { get; set; } = "https://resources.download.minecraft.net";

    /// <summary>
    ///     Gets or sets the base URL of the player configuration API. Skin and cape operations append
    ///     paths such as <c>/skins</c> and <c>/capes/active</c> to this value.
    /// </summary>
    [JsonPropertyName("PlayerConfigUrl")]
    public string PlayerConfigUrl { get; set; } = "https://api.minecraftservices.com/minecraft/profile";
}
