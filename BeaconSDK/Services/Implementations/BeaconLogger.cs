using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Services.Implementations;

/// <summary>
///     Default implementation of <see cref="IBeaconLogger" /> that discards entries below a minimum level and writes the
///     rest to an <see cref="ILogger{TCategoryName}" />, the console, or both.
/// </summary>
/// <remarks>
///     When an <see cref="ILogWriter" /> is supplied, every emitted message is also enqueued for asynchronous file
///     persistence. Console writes are guarded by a lock so concurrent calls cannot interleave.
/// </remarks>
public class BeaconLogger : IBeaconLogger
{
    private readonly ILogWriter? _logWriter;
    private readonly ILogger? _msLogger;
    private readonly Lock _logLock = new();

    private readonly string? _customLogFilePath;
    private readonly LogLevel _logLevel;
    private readonly string _moduleName;
    private readonly bool _printLogs;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BeaconLogger" /> class.
    /// </summary>
    /// <param name="moduleName">Name of the module or component producing logs (used in output).</param>
    /// <param name="logLevel">Minimum <see cref="LogLevel" /> that will be emitted by this logger.</param>
    /// <param name="printLogs">If true, entries are written to the console or to <paramref name="logger" /> when one is set.</param>
    /// <param name="customLogFilePath">
    ///     Optional log file path. When null or empty, <paramref name="logWriter" /> falls back to its configured default,
    ///     and file logging is skipped if that default is also unset.
    /// </param>
    /// <param name="logWriter">Optional <see cref="ILogWriter" /> used to persist entries to a file.</param>
    /// <param name="logger">
    ///     Optional <see cref="ILogger{TCategoryName}" /> that receives entries instead of them being written to the console.
    /// </param>
    public BeaconLogger(string moduleName, LogLevel logLevel, bool printLogs = true, string? customLogFilePath = null, ILogWriter? logWriter = null, ILogger? logger = null)
    {
        _moduleName = moduleName;
        _logLevel = logLevel;
        _printLogs = printLogs;
        _customLogFilePath = customLogFilePath;
        _logWriter = logWriter;
        _msLogger = logger;
    }

    #region IBeaconLogger Implementation

    /// <inheritdoc />
    public void Log(LogLevel logLevel, LogEntry entry, Exception? exception,
        Func<LogEntry, Exception?, string> formatter)
    {
        if (_logLevel > logLevel)
            return;

        var text = formatter(entry, exception);
        if (_printLogs)
        {
            if (_msLogger != null)
            {
                _msLogger.Log(logLevel, exception, entry.Message);
            }
            else
            {
                lock (_logLock)
                {
                    try
                    {
                        Console.ForegroundColor = GetLogLevelColor(logLevel);
                        Console.WriteLine(text);
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Logger Console Error] {ex.Message}");
                    }
                }
            }
        }

        _logWriter?.EnqueueLog(text, _customLogFilePath);
    }

    /// <inheritdoc />
    public string ModuleName => _moduleName;

    /// <inheritdoc />
    public LogLevel LogLevel => _logLevel;

    #endregion

    private ConsoleColor GetLogLevelColor(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Debug => ConsoleColor.Magenta,
            LogLevel.Information => ConsoleColor.Cyan,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
    }
}

/// <summary>
///     A <see cref="BeaconLogger" /> whose module name is derived from <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">The component type the logger reports as its module name.</typeparam>
public class BeaconLogger<T> : BeaconLogger, IBeaconLogger<T> where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BeaconLogger{T}" /> class from the SDK configuration.
    ///     This is the constructor dependency injection uses.
    /// </summary>
    /// <param name="logWriter">The <see cref="ILogWriter" /> used to persist entries to a file.</param>
    /// <param name="logger">
    ///     The <see cref="ILogger{TCategoryName}" /> resolved for <typeparamref name="T" />. It is only used when
    ///     <see cref="LoggingConfig.EnableCustomLogger" /> is false; otherwise output goes to the console.
    /// </param>
    /// <param name="config">
    ///     The Beacon SDK configuration. <see cref="LoggingConfig.LogLevel" /> becomes the minimum emitted level and
    ///     <see cref="LoggingConfig.DefaultLogFilePath" /> the target log file.
    /// </param>
    public BeaconLogger(ILogWriter? logWriter, ILogger<T>? logger, BeaconConfiguration config)
        : this(config.Logging.LogLevel, customLogFilePath: config.Logging.DefaultLogFilePath, logWriter: logWriter, logger: config.Logging.EnableCustomLogger ? null : logger) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BeaconLogger{T}" /> class.
    /// </summary>
    /// <param name="logLevel">Minimum level to log.</param>
    /// <param name="printLogs">Whether entries are written to the console or to <paramref name="logger" />.</param>
    /// <param name="customLogFilePath">Optional custom file path for persisted entries.</param>
    /// <param name="logWriter">Optional <see cref="ILogWriter" /> used to persist entries to a file.</param>
    /// <param name="logger">
    ///     Optional <see cref="ILogger{TCategoryName}" /> that receives entries instead of them being written to the console.
    /// </param>
    public BeaconLogger(LogLevel logLevel, bool printLogs = true, string? customLogFilePath = null, ILogWriter? logWriter = null, ILogger<T>? logger = null)
        : base(typeof(T).Name, logLevel, printLogs, customLogFilePath, logWriter, logger)
    {
    }
}
