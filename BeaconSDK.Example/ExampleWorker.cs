using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Domain;
using Tavstal.BeaconSDK.Instances;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Endpoints;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Instance.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;

// ReSharper disable ClassNeverInstantiated.Global

namespace Tavstal.BeaconSDK.Example;

public class ExampleWorker : IHostedService, IProgressReporter
{
    private readonly IHttpService _httpService;
    private readonly IInstanceInstallService _instanceInstallService;
    private readonly IInstanceLaunchService _instanceLaunchService;
    private readonly IJavaService _javaService;
    private readonly IBeaconLogger<ExampleWorker> _logger;
    private readonly IManifestService _manifestService;

    public ExampleWorker(IBeaconLogger<ExampleWorker> logger, IHttpService httpService, IJavaService javaService,
        IInstanceLaunchService instanceLaunchService, IInstanceInstallService instanceInstallService,
        IManifestService manifestService)
    {
        _logger = logger;
        _httpService = httpService;
        _javaService = javaService;
        _instanceLaunchService = instanceLaunchService;
        _instanceInstallService = instanceInstallService;
        _manifestService = manifestService;
    }

    public async Task StartAsync(CancellationToken cts)
    {
        var workingDir = Directory.GetCurrentDirectory();
        var manifestPath = Path.Combine(workingDir, "manifest.json");
        _logger.LogInformation("Example worker started");
        _logger.LogInformation("Fetching minecraft manifest...");
        if (!File.Exists(manifestPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _logger.LogInformation($"Downloading minecraft manifest {e:0.00}%...");
            };
            await _httpService.DownloadFileAsync(MicrosoftEndpoints.MinecraftManifestUrl, manifestPath, null, progress, cts);
        }

        var manifest = await _manifestService.GetMinecraftManifestAsync(manifestPath, cts);
        if (manifest == null)
        {
            _logger.LogError("Failed to fetch minecraft manifest. Please try again.");
            return;
        }

        _logger.LogInformation("Please provide your minecraft username:");
        string? minecraftUsername;
        while (true)
        {
            minecraftUsername = Console.ReadLine();
            if (string.IsNullOrEmpty(minecraftUsername))
            {
                _logger.LogInformation("Please provide a valid minecraft username:");
                continue;
            }

            if (minecraftUsername.Length < 3 || minecraftUsername.Length > 16)
            {
                _logger.LogInformation("Please provide a username that is between 3 and 16 characters:");
                continue;
            }

            break;
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
        var gameInstanceDir = Path.Combine(instancesDir, minecraftVersion);
        Directory.CreateDirectory(gameInstanceDir);

        var gameDetails = new GameDetails(
            string.Empty,
            1024,
            4096,
            string.Empty,
            targetVersion.Id,
            EMinecraftKind.VANILLA,
            null,
            gameInstanceDir,
            string.Empty,
            [],
            string.Empty,
            [],
            null
        );
        var pathDetails = new PathDetails(assetsDir, cacheDir, libsDir, versionsDir, manifestPath, null, []);
        var launcherDetails = new LauncherDetails("exampleApp", "1.0.0");
        var uuid = GameHelper.GetOfflinePlayerUUID(minecraftUsername);
        var clientDetails = new ClientDetails(null, minecraftUsername, uuid, true);

        var config = new InstanceConfiguration
        {
          Version  = targetVersion,
          Game = gameDetails,
          Path = pathDetails,
          Launcher = launcherDetails,
          Client = clientDetails
        };

        var instance = new MinecraftInstance(minecraftVersion, config, new BeaconLogger<MinecraftInstance>(_logger.GetLogLevel()), this);
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
        _logger.LogInformation("Example worker stopped");
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
