using System.Net.Security;
using System.Security.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tavstal.BeaconSDK.Models.Config;
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
    ///     Registers the Beacon SDK services in the dependency injection container.
    ///     This includes the SDK's own services, data protection, and the Minecraft and launcher services.
    ///     The supplied <paramref name="configuration" /> is registered as a singleton so SDK services can resolve it.
    /// </summary>
    /// <remarks>
    ///     The <see cref="LogWriter" /> is registered as a singleton, as an <see cref="ILogWriter" />, and as a hosted
    ///     service, so entries are only written to disk while the host is running.
    ///     A named <see cref="HttpClient" /> called "default" is also registered, pre-configured with the user agent,
    ///     accept header and timeout from <see cref="Tavstal.BeaconSDK.Models.Config.HttpConfig" />, and a primary
    ///     <see cref="SocketsHttpHandler" /> built from the configured SSL protocols, automatic decompression, redirect
    ///     and HTTP/2 settings.
    /// </remarks>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configuration">The Beacon SDK configuration providing endpoint, HTTP, authentication and data protection settings.</param>
    /// <returns>The same <paramref name="services" /> instance to allow chaining.</returns>
    public static IServiceCollection AddBeaconServices(this IServiceCollection services, BeaconConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddSingleton<LogWriter>();
        services.AddSingleton<ILogWriter>(sp => sp.GetRequiredService<LogWriter>());
        services.AddHostedService(sp => sp.GetRequiredService<LogWriter>());

        // BeaconLogger<T> optionally forwards to ILogger<T>. Registering the open generic lets the
        // container satisfy that constructor even when the host never called AddLogging().
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));

        services.AddSingleton(typeof(IBeaconLogger<>), typeof(BeaconLogger<>));
        services.AddHttpClient("default", client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(configuration.Http.Agent);
                client.DefaultRequestHeaders.Accept.ParseAdd(configuration.Http.AcceptHeader);
                client.Timeout = configuration.Http.Timeout;
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var sockets = new SocketsHttpHandler
                {
                    AutomaticDecompression = configuration.Http.AutomaticDecompression,
                    AllowAutoRedirect = configuration.Http.AllowAutoRedirect,
                    EnableMultipleHttp2Connections = configuration.Http.EnableMultipleHttp2Connections
                };

                if (configuration.Http.UseTls12Protocol)
                    sockets.SslOptions = new SslClientAuthenticationOptions { EnabledSslProtocols = SslProtocols.Tls12 };

                return sockets;
            });
        services.AddSingleton<IHttpService, HttpService>();

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
