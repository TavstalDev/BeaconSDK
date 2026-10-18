using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Tavstal.BeaconSDK.Models.Config;

/// <summary>
///     Represents the logging settings used by the loggers the SDK creates through dependency injection.
/// </summary>
public class LoggingConfig
{
    /// <summary>
    ///     Gets or sets the minimum level emitted by dependency injected loggers. Entries below this level are discarded.
    /// </summary>
    [JsonPropertyName("LogLevel")]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    ///     Gets or sets a value indicating whether loggers hand their output to an injected <see cref="ILogger{TCategoryName}" />.
    ///     When false, the custom logger is used instead.
    /// </summary>
    [JsonPropertyName("EnableCustomLogger")]
    public bool EnableCustomLogger { get; set; } = true;

    /// <summary>
    ///     Gets or sets the log file the SDK-created loggers write to. Null or empty disables file logging.
    /// </summary>
    [JsonPropertyName("DefaultLogFilePath")]
    public string? DefaultLogFilePath { get; set; } = null;
}