using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;
using Tavstal.BeaconSDK.Services.Abstractions.Auth;
using Tavstal.BeaconSDK.Services.Implementations;
using Tavstal.BeaconSDK.Tests.Fakes;
using Tavstal.BeaconSDK.Tests.Fixtures;

namespace Tavstal.BeaconSDK.Tests.Services;

[Collection("TempDir")]
public class BeaconBootstrapTests : IDisposable
{
    private readonly string _keysDir;
    private readonly string _logDir;

    public BeaconBootstrapTests()
    {
        _keysDir = Path.Combine(Path.GetTempPath(), $"beacon-keys-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_keysDir);
        _logDir = Path.Combine(Path.GetTempPath(), $"beacon-logs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_logDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_keysDir))
                Directory.Delete(_keysDir, true);
            if (Directory.Exists(_logDir))
                Directory.Delete(_logDir, true);
        }
        catch
        {
            /* best effort */
        }
    }

    private ServiceProvider BuildProvider(Action<BeaconConfiguration>? configure = null)
    {
        var config = ModelFactory.CreateAppConfiguration();
        config.DataProtection.KeysDirectory = _keysDir;
        configure?.Invoke(config);
        return new ServiceCollection().AddBeaconServices(config).BuildServiceProvider();
    }

    [Fact(DisplayName = "AddBeaconServices registers all expected service types")]
    public void AddBeaconServices_RegistersAllExpectedServiceTypes()
    {
        using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<IBeaconLogger<IHttpService>>());
        Assert.NotNull(provider.GetRequiredService<IHttpService>());
        Assert.NotNull(provider.GetRequiredService<IEncryptionService>());
        Assert.NotNull(provider.GetRequiredService<IManifestService>());
        Assert.NotNull(provider.GetRequiredService<ILibraryDownloadService>());
        Assert.NotNull(provider.GetRequiredService<IInstanceInstallService>());
        Assert.NotNull(provider.GetRequiredService<IInstanceLaunchService>());
        Assert.NotNull(provider.GetRequiredService<IMojangSkinService>());
        Assert.NotNull(provider.GetRequiredService<IMicrosoftAuthService>());
        Assert.NotNull(provider.GetRequiredService<IMicrosoftDeviceAuthService>());
        Assert.NotNull(provider.GetRequiredService<IMicrosoftHttpAuthService>());
        Assert.NotNull(provider.GetRequiredService<IJavaService>());
    }

    [Fact(DisplayName = "Resolved encryption service uses persisted keys from the temp key ring")]
    public void ResolvedEncryptionService_UsesPersistedKeys_FromTempKeyRing()
    {
        using var provider = BuildProvider();
        var encryption = provider.GetRequiredService<IEncryptionService>();
        var plain = "persisted-secret"u8.ToArray();

        var cipher = encryption.Encrypt(plain);

        Assert.Equal(plain, encryption.Decrypt(cipher));
        Assert.NotEmpty(Directory.EnumerateFiles(_keysDir, "*.xml"));
    }

    [Fact(DisplayName = "Resolved services are singletons returning the same instance on repeated resolve")]
    public void ResolvedServices_AreSingletons_SameInstanceOnRepeatedResolve()
    {
        using var provider = BuildProvider();

        Assert.Same(provider.GetRequiredService<IHttpService>(),
            provider.GetRequiredService<IHttpService>());
        Assert.Same(provider.GetRequiredService<IEncryptionService>(),
            provider.GetRequiredService<IEncryptionService>());
        Assert.Same(provider.GetRequiredService<IMicrosoftAuthService>(),
            provider.GetRequiredService<IMicrosoftAuthService>());
    }

    [Fact(DisplayName = "LogWriter resolves as a singleton shared by ILogWriter and the hosted service")]
    public void LogWriter_IsSharedSingleton_AcrossILogWriterAndHostedService()
    {
        using var provider = BuildProvider();

        var logWriter = provider.GetRequiredService<LogWriter>();
        var hosted = provider.GetServices<IHostedService>().OfType<LogWriter>().Single();

        Assert.Same(logWriter, provider.GetRequiredService<ILogWriter>());
        Assert.Same(logWriter, hosted);
    }

    [Fact(DisplayName = "AddBeaconServices registers a logger without requiring AddLogging")]
    public void AddBeaconServices_ResolvesBeaconLogger_WithoutAddLogging()
    {
        using var provider = BuildProvider();

        var logger = provider.GetRequiredService<IBeaconLogger<IHttpService>>();

        Assert.NotNull(logger);
        Assert.Equal(nameof(IHttpService), logger.ModuleName);
    }

    [Fact(DisplayName = "Resolved BeaconLogger adopts the log level from the logging configuration")]
    public void ResolvedBeaconLogger_UsesConfiguredLogLevel()
    {
        using var provider = BuildProvider(config => config.Logging.LogLevel = LogLevel.Warning);

        var logger = provider.GetRequiredService<IBeaconLogger<IHttpService>>();

        Assert.Equal(LogLevel.Warning, logger.LogLevel);
    }

    [Fact(DisplayName = "Resolved BeaconLogger discards entries below the configured log level")]
    public void ResolvedBeaconLogger_DiscardsEntriesBelowConfiguredLevel()
    {
        var writer = new CapturingLogWriter();
        using var provider = BuildProviderWithWriter(config =>
        {
            config.Logging.LogLevel = LogLevel.Warning;
        }, writer);

        var logger = provider.GetRequiredService<IBeaconLogger<IHttpService>>();
        logger.LogInformation("below-threshold");

        Assert.Empty(writer.Entries);
    }

    [Fact(DisplayName = "Resolved BeaconLogger forwards entries at or above the configured log level to the log writer")]
    public void ResolvedBeaconLogger_ForwardsEntriesAtOrAboveLevel_ToLogWriter()
    {
        var writer = new CapturingLogWriter();
        using var provider = BuildProviderWithWriter(config =>
        {
            config.Logging.LogLevel = LogLevel.Warning;
        }, writer);

        var logger = provider.GetRequiredService<IBeaconLogger<IHttpService>>();
        logger.LogWarning("at-threshold");

        var entry = Assert.Single(writer.Entries);
        Assert.Contains("at-threshold", entry.Entry);
        Assert.Null(entry.Path);
    }

    [Fact(DisplayName = "EnableCustomLogger false routes resolved BeaconLogger output to ILogger<T>")]
    public void EnableCustomLoggerFalse_ForwardsToInjectedILogger()
    {
        var capturing = new CapturingLogger<IHttpService>();
        using var provider = BuildProviderWithLogger(config =>
        {
            config.Logging.EnableCustomLogger = false;
            config.Logging.LogLevel = LogLevel.Trace;
        }, capturing);

        var logger = provider.GetRequiredService<IBeaconLogger<IHttpService>>();
        logger.LogError("forwarded-to-ms-logger");

        var entry = Assert.Single(capturing.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("forwarded-to-ms-logger", entry.Message);
    }

    [Fact(DisplayName = "EnableCustomLogger true keeps resolved BeaconLogger off the injected ILogger<T>")]
    public void EnableCustomLoggerTrue_DoesNotForwardToInjectedILogger()
    {
        var capturing = new CapturingLogger<IHttpService>();
        using var provider = BuildProviderWithLogger(config =>
        {
            config.Logging.EnableCustomLogger = true;
            config.Logging.LogLevel = LogLevel.Trace;
        }, capturing);

        var logger = provider.GetRequiredService<IBeaconLogger<IHttpService>>();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(TextWriter.Null);
            logger.LogError("written-to-console");
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.Empty(capturing.Entries);
    }

    [Fact(DisplayName = "LogWriter discards entries when no path is supplied and no default is configured")]
    public void LogWriter_WithoutAnyPath_DiscardsEntry()
    {
        using var provider = BuildProvider(config => config.Logging.DefaultLogFilePath = null);

        var writer = provider.GetRequiredService<ILogWriter>();
        writer.EnqueueLog("dropped");

        Assert.Empty(Directory.EnumerateFiles(_logDir));
    }

    [Fact(DisplayName = "LogWriter writes enqueued entries to the configured default log file once started")]
    public async Task LogWriter_AfterStart_WritesEntryToConfiguredDefaultPath()
    {
        var logPath = Path.Combine(_logDir, "beacon.log");
        await using var provider = BuildProvider(config => config.Logging.DefaultLogFilePath = logPath);
        using var cts = new CancellationTokenSource();

        var hosted = provider.GetServices<IHostedService>().OfType<LogWriter>().Single();
        await hosted.StartAsync(cts.Token);

        var writer = provider.GetRequiredService<ILogWriter>();
        writer.EnqueueLog("persisted-entry");

        await WaitForFileAsync(logPath, "persisted-entry");

        await hosted.StopAsync(CancellationToken.None);
        await cts.CancelAsync();
    }

    private ServiceProvider BuildProviderWithWriter(Action<BeaconConfiguration> configure, CapturingLogWriter writer)
    {
        return BuildProvider(configure, services => services.AddSingleton<ILogWriter>(writer));
    }

    private ServiceProvider BuildProviderWithLogger(Action<BeaconConfiguration> configure, ILogger<IHttpService> logger)
    {
        return BuildProvider(configure, services => services.AddSingleton(logger));
    }

    private ServiceProvider BuildProvider(Action<BeaconConfiguration> configure,
        Action<IServiceCollection> configureServices)
    {
        var config = ModelFactory.CreateAppConfiguration();
        config.DataProtection.KeysDirectory = _keysDir;
        configure(config);

        var services = new ServiceCollection();
        services.AddBeaconServices(config);
        configureServices(services);
        return services.BuildServiceProvider();
    }

    private static async Task WaitForFileAsync(string path, string expected)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (File.Exists(path) && (await File.ReadAllTextAsync(path)).Contains(expected))
                return;
            await Task.Delay(50);
        }

        Assert.Fail($"Timed out waiting for '{expected}' to be written to '{path}'.");
    }
}
