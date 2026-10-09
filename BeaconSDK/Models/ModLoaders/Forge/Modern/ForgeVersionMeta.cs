using System.Text.Json.Serialization;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;

namespace Tavstal.BeaconSDK.Models.ModLoaders.Forge.Modern;

/// <summary>
///     Represents metadata for a specific version of the Forge mod loader.
/// </summary>
public class ForgeVersionMeta
{
    /// <summary>
    ///     Gets or sets the arguments metadata for the Forge version.
    /// </summary>
    [JsonPropertyName("arguments")]
    public ArgumentMeta Arguments { get; set; } = new();

    /// <summary>
    ///     Gets or sets the Minecraft arguments for the Forge version.
    /// </summary>
    [JsonPropertyName("minecraftArguments")]
    public string? MinecraftArguments { get; set; }

    /// <summary>
    ///     Gets or sets the unique identifier of the Forge version.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the version of Minecraft that this Forge version inherits from.
    /// </summary>
    [JsonPropertyName("inheritsFrom")]
    public string InheritsFrom { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the list of libraries required by the Forge version.
    /// </summary>
    [JsonPropertyName("libraries")]
    public List<LibraryMeta> Libraries { get; set; } = [];

    /// <summary>
    ///     Gets or sets the logging metadata for the Forge version.
    /// </summary>
    [JsonPropertyName("logging")]
    public LoggingMeta LoggingMeta { get; set; } = new();

    /// <summary>
    ///     Gets or sets the main class to be executed for the Forge version.
    /// </summary>
    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the type of the Forge version (e.g., "release", "snapshot").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
