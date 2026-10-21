using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Domain;
using Tavstal.BeaconSDK.Instances;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Instance.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.ModLoaders;
using Tavstal.BeaconSDK.Services.Abstractions;
using Tavstal.BeaconSDK.Services.Implementations;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Tavstal.BeaconSDK.Example;

public class ExampleFabricWorker : IHostedService, IProgressReporter
{
    private readonly IBeaconLogger<ExampleFabricWorker> _logger;
    private readonly ExampleLoginWorker _loginWorker;
    private readonly IJavaService _javaService;
    private readonly IInstanceLaunchService _instanceLaunchService;
    private readonly IInstanceInstallService _instanceInstallService;
    private readonly IManifestService _manifestService;
    private readonly BeaconConfiguration _appConfiguration;

    public ExampleFabricWorker(IBeaconLogger<ExampleFabricWorker> logger, ExampleLoginWorker loginWorker, IJavaService javaService,
        IInstanceLaunchService instanceLaunchService, IInstanceInstallService instanceInstallService,
        IManifestService manifestService, BeaconConfiguration appConfiguration)
    {
        _logger = logger;
        _loginWorker = loginWorker;
        _javaService = javaService;
        _instanceLaunchService = instanceLaunchService;
        _instanceInstallService = instanceInstallService;
        _manifestService = manifestService;
        _appConfiguration = appConfiguration;
    }

    public async Task StartAsync(CancellationToken cts)
    {
        if (string.IsNullOrEmpty(_loginWorker.Username) || string.IsNullOrEmpty(_loginWorker.Uuid))
        {
            _logger.LogError("Can not process further due to failed login.");
            return;
        }

        var workingDir = Directory.GetCurrentDirectory();
        var manifestPath = Path.Combine(workingDir, "manifest.json");
        var fabricManifestPath = Path.Combine(workingDir, "fabric.json");
        _logger.LogInformation("Example worker started");
        _logger.LogInformation("Fetching minecraft manifest...");
        if (!File.Exists(manifestPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _logger.LogInformation($"Downloading minecraft manifest {e:0.00}%...");
            };
            await _manifestService.DownloadMinecraftManifestAsync(manifestPath, progress, cts);
        }

        var manifest = await _manifestService.GetMinecraftManifestAsync(manifestPath, cts);
        if (manifest == null)
        {
            _logger.LogError("Failed to fetch minecraft manifest. Please try again.");
            return;
        }

        _logger.LogInformation("Please provide a minecraft version to start:");
        string? minecraftVersion;
        while (true)
        {
            minecraftVersion = Console.ReadLine();
            if (string.IsNullOrEmpty(minecraftVersion))
            {
                _logger.LogError("You must provide a valid minecraft version.");
                continue;
            }

            break;
        }

        var targetVersion = manifest.Versions.FirstOrDefault(x => x.Id == minecraftVersion);
        if (targetVersion == null)
        {
            _logger.LogError("The manifest does not contain the provided minecraft version.");
            return;
        }

        List<IModManifest>? fabricManifest;
        if (!File.Exists(fabricManifestPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _logger.LogInformation($"Downloading fabric manifest {e:0.00}%...");
            };
            fabricManifest = await _manifestService.DownloadFabricManifestAsync(fabricManifestPath, progress, cts);
        }
        else
            fabricManifest = await _manifestService.GetFabricManifestAsync(fabricManifestPath, cts);

        if (fabricManifest == null || fabricManifest.Count == 0)
        {
            _logger.LogError("Failed to fetch fabric manifest. Please try again.");
            return;
        }

        var gameDir = Path.Combine(workingDir, "game");
        Directory.CreateDirectory(gameDir);
        var assetsDir = Path.Combine(gameDir, "assets");
        Directory.CreateDirectory(assetsDir);
        var cacheDir = Path.Combine(gameDir, "cache");
        Directory.CreateDirectory(cacheDir);
        var libsDir = Path.Combine(gameDir, "libs");
        Directory.CreateDirectory(libsDir);
        var versionsDir = Path.Combine(gameDir, "versions");
        Directory.CreateDirectory(versionsDir);
        var javaDir = Path.Combine(gameDir, "java");
        Directory.CreateDirectory(javaDir);
        var instancesDir = Path.Combine(gameDir, "instances");
        Directory.CreateDirectory(instancesDir);
        var gameInstanceDir = Path.Combine(instancesDir, minecraftVersion + "-fabric");
        Directory.CreateDirectory(gameInstanceDir);

        var fabricVerion = fabricManifest.First();
        var gameDetails = new GameDetails(
            string.Empty,
            1024,
            4096,
            string.Empty,
            targetVersion.Id,
            EMinecraftKind.FABRIC,
            fabricVerion.Version,
            gameInstanceDir,
            string.Empty,
            [],
            string.Empty,
            [],
            null
        );
        var pathDetails = new PathDetails(assetsDir, cacheDir, libsDir, versionsDir, []);
        var launcherDetails = new LauncherDetails("exampleApp", "1.0.0");
        var clientDetails = new ClientDetails(_loginWorker.Token, _loginWorker.Username, _loginWorker.Uuid, string.IsNullOrEmpty(_loginWorker.Token));

        var config = new InstanceConfiguration
        {
          Version  = targetVersion,
          Game = gameDetails,
          Path = pathDetails,
          Launcher = launcherDetails,
          Client = clientDetails
        };

        var instance = new FabricInstance(minecraftVersion, _appConfiguration, config, new BeaconLogger<MinecraftInstance>(_logger.LogLevel), this);
        var installResult = await _instanceInstallService.InstallAsync(instance, this, cts);
        if (!installResult)
        {
            _logger.LogError("Failed to install minecraft instance.");
            return;
        }

        var requiredJavaVersion = instance.MinecraftVersionMeta.JavaVersionMeta?.MajorVersion ?? 8;
        await _javaService.LocateJavaInstallationsAsync(javaDir, cancellationToken: cts);
        var javaPath = await _javaService.GetJavaVersionAsync(requiredJavaVersion, cts);
        if (string.IsNullOrEmpty(javaPath))
        {
            bool alertResult;
            while (true)
            {
                _logger.LogInformation("The required java version is missing. Do you want to install it? y/n");
                var c = Console.ReadKey().KeyChar;
                if (c == 'y')
                {
                    alertResult = true;
                    break;
                }

                if (c == 'n')
                {
                    alertResult = false;
                    break;
                }
            }

            if (!alertResult)
            {
                _logger.LogWarning("The instance can not be started without the required java version.");
                return;
            }

            var prog = new Progress<double>(e =>
            {
                _logger.LogInformation($"Downloading java {requiredJavaVersion} {e:0.00}%...");
            });

            javaPath = await _javaService.DownloadJavaVersionAsync(requiredJavaVersion,
                javaDir, "java-mirrors.json", prog, cts);

            if (string.IsNullOrEmpty(javaPath))
            {
                _logger.LogError("Failed to install the required java version.");
                return;
            }
        }

        instance.Config.Game.JavaPath = javaPath;

        var process = await _instanceLaunchService.LaunchAsync(instance, this, cts);
        if (process == null)
        {
            _logger.LogError("Failed to launch the  Process is null.");
            return;
        }

        process.Exited += (_, _) =>
        {
            _logger.LogInformation("The process has exited.");
        };
    }

    public Task StopAsync(CancellationToken cts)
    {
        _logger.LogInformation($"{nameof(ExampleFabricWorker)} has stopped");
        return Task.CompletedTask;
    }

    #region IProgressReporter Implementation

    public void Report(double progress)
    {
        _logger.LogInformation($"Progress: {progress:0.00}%");
    }

    public void Report(double progress, string fileName, bool downloading = true)
    {
        _logger.LogInformation(downloading
            ? $"Downloading {fileName} {progress:0.00}%"
            : $"Reading {fileName} {progress:0.00}%");
    }

    public void Report(EProgressKind kind, double progress)
    {
        _logger.LogInformation($"{kind}: {progress:0.00}%");
    }

    public void ReportExpectedTask(int? count) { }

    public void ReportTaskCompleted() { }

    #endregion
}
