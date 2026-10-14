using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK.Helpers;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;
using Tavstal.BeaconSDK.Services.Abstractions.Auth;
using Tavstal.BeaconSDK.Services.Implementations;
using Tavstal.BeaconSDK.Services.Implementations.Auth;

namespace Tavstal.BeaconSDK;

/// <summary>
///     Provides extension methods for registering the Beacon SDK services and completing host setup.
/// </summary>
public static class BeaconBootstrap
{
    /// <summary>
    ///     Gets the task running the background log writer loop.
    ///     The task is created by <see cref="SetupLogger" /> and is <see cref="Task.CompletedTask" /> until then.
    /// </summary>
    public static Task LoggerTask { get; private set; } = Task.CompletedTask;

    /// <summary>
    ///     Starts the background log writer loop used by <see cref="LoggerHelper" />.
    ///     Calling this method more than once has no effect, the existing loop is kept.
    /// </summary>
    /// <param name="hostBuilder">The host builder that is being configured.</param>
    /// <param name="cancellationToken">Cancellation token used to stop the background log writer.</param>
    /// <returns>The same <paramref name="hostBuilder" /> instance to allow chaining.</returns>
    public static IHostBuilder SetupLogger(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default)
    {
        if (LoggerTask != Task.CompletedTask)
            return hostBuilder;
        LoggerTask = Task.Run(() => LoggerHelper.ProcessLogQueueAsync(cancellationToken), cancellationToken);
        return hostBuilder;
    }

    /// <summary>
    ///     Registers the Beacon SDK services in the dependency injection container.
    ///     This includes the SDK's own services, data protection, and the Minecraft and launcher services.
    ///     The supplied <paramref name="configuration" /> is registered as a singleton so SDK services can resolve it.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configuration">The Beacon SDK configuration providing endpoint, HTTP, authentication and data protection settings.</param>
    /// <returns>The same <paramref name="services" /> instance to allow chaining.</returns>
    public static IServiceCollection AddBeaconServices(this IServiceCollection services, BeaconConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddSingleton(typeof(IBeaconLogger<>), typeof(BeaconLogger<>));
        services.AddSingleton<IHttpService, HttpService>();
        //var keyDir = Path.Combine(appDir, "keys");
        //Directory.CreateDirectory(keyDir);
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(configuration.DataProtection.KeysDirectory))
            .SetApplicationName(configuration.DataProtection.ApplicationName);
        services.AddSingleton<IEncryptionService, EncryptionService>();

        // Minecraft services
        services.AddSingleton<IManifestService, ManifestService>();
        services.AddSingleton<ILibraryDownloadService, LibraryDownloadService>();
        services.AddSingleton<IInstanceInstallService, InstanceInstallService>();
        services.AddSingleton<IInstanceLaunchService, InstanceLaunchService>();

        services.AddSingleton<IMojangSkinService, MojangSkinService>();
        services.AddSingleton<IMicrosoftAuthService, MicrosoftAuthService>();
        services.AddSingleton<IMicrosoftDeviceAuthService, MicrosoftDeviceAuthService>();
        services.AddSingleton<IMicrosoftHttpAuthService, MicrosoftHttpAuthService>();

        // Launcher services
        services.AddSingleton<IJavaService, JavaService>();

        return services;
    }
}
