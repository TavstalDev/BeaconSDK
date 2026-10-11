using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Tavstal.BeaconSDK.Helpers;

namespace Tavstal.BeaconSDK.Models.Logging;

/// <inheritdoc />
public class BeaconLogger : IBeaconLogger
{
    private readonly string? _customLogFilePath;
    private readonly LogLevel _logLevel;
    private readonly Lock _logLock = new();
    private readonly string _moduleName;
    private readonly bool _printLogs;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BeaconLogger" /> class.
    /// </summary>
    /// <param name="moduleName">Name of the module or component producing logs (used in output).</param>
    /// <param name="logLevel">Minimum <see cref="LogLevel" /> that will be emitted by this logger.</param>
    /// <param name="printLogs">If true, log messages are also printed to the console.</param>
    /// <param name="customLogFilePath">Optional path to a custom log file (passed to <see cref="LoggerHelper" />).</param>
    public BeaconLogger(string moduleName, LogLevel logLevel, bool printLogs = true, string? customLogFilePath = null)
    {
        _moduleName = moduleName;
        _logLevel = logLevel;
        _printLogs = printLogs;
        _customLogFilePath = customLogFilePath;
    }

    /// <inheritdoc />
    public void Log(LogLevel logLevel, LogEntry entry, Exception? exception,
        Func<LogEntry, Exception?, string> formatter)
    {
        if (_logLevel > logLevel)
            return;

        var text = formatter(entry, exception);
        if (_printLogs)
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

        LoggerHelper.EnqueueLog(text, _customLogFilePath);
    }

    /// <inheritdoc />
    public string GetModuleName()
    {
        return _moduleName;
    }

    /// <inheritdoc />
    public LogLevel GetLogLevel()
    {
        return _logLevel;
    }

    /// <summary>
    ///     Maps a <see cref="LogLevel" /> to a <see cref="ConsoleColor" /> for console output.
    /// </summary>
    /// <param name="logLevel">The log level to map.</param>
    /// <returns>A <see cref="ConsoleColor" /> suitable for the given level.</returns>
    private static ConsoleColor GetLogLevelColor(LogLevel logLevel)
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

/// <inheritdoc cref="IBeaconLogger" />
public class BeaconLogger<T> : BeaconLogger, IBeaconLogger<T> where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BeaconLogger{T}" /> class with default settings.
    ///     Used by dependency injection to instantiate loggers without explicit parameter passing.
    /// </summary>
#if DEBUG
    public BeaconLogger()
        : this(LogLevel.Debug)
    {
    }
#else
    public BeaconLogger()
        : this(LogLevel.Information) { }
#endif

    /// <summary>
    ///     Initializes a new instance of the <see cref="BeaconLogger{T}" /> class.
    /// </summary>
    /// <param name="logLevel">Minimum level to log.</param>
    /// <param name="printLogs">Whether messages are printed to the console.</param>
    /// <param name="customLogFilePath">Optional custom file path for persisted logs.</param>
    public BeaconLogger(LogLevel logLevel, bool printLogs = true, string? customLogFilePath = null)
        : base(typeof(T).Name, logLevel, printLogs, customLogFilePath)
    {
    }
}
