using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.MojangApi.Meta;

/// <summary>
///     Represents metadata for logging configuration in Minecraft.
/// </summary>
public class LoggingMeta
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggingMeta" /> class.
    /// </summary>
    public LoggingMeta()
    {
        Client = new LoggingClient();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggingMeta" /> class with a specified logging client configuration.
    /// </summary>
    /// <param name="client">The logging client configuration.</param>
    public LoggingMeta(LoggingClient client)
    {
        Client = client;
    }

    /// <summary>
    ///     Gets or sets the logging client configuration.
    /// </summary>
    [JsonPropertyName("client")]
    public LoggingClient Client { get; set; }
}
