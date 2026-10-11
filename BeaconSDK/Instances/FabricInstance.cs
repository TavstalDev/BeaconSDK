using System.Text.Json;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Helpers.Serialization;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Endpoints.Modding;
using Tavstal.BeaconSDK.Models.Installer;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.ModLoaders.Fabric;
using Tavstal.BeaconSDK.Models.MojangApi;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;
using Tavstal.BeaconSDK.Services.Abstractions;
using CoreJsonContext = Tavstal.BeaconSDK.Models.Json.CoreJsonContext;

namespace Tavstal.BeaconSDK.Instances;

/// <summary>
///     An instance implementation that installs and configures Fabric-based modded instances.
/// </summary>
/// <param name="id">Unique identifier for the instance.</param>
/// <param name="gameVersion">The vanilla <see cref="MinecraftVersion" /> used as the base for this instance.</param>
/// <param name="gameDetails">Game/runtime settings describing version strings and custom paths.</param>
/// <param name="pathDetails">Filesystem root paths used by the instance (libraries, assets, versions, etc.).</param>
/// <param name="launcherDetails">Launcher-level metadata and settings.</param>
/// <param name="clientDetails">Client-specific data (credentials, tokens, etc.).</param>
/// <param name="logger">Logger used to emit diagnostic messages during installation and setup.</param>
/// <param name="resolution">Optional default window resolution for the instance.</param>
/// <param name="progressReporter">Optional progress reporter used to report long-running download/extract progress.</param>
public class FabricInstance(
    string id,
    MinecraftVersion gameVersion,
    GameDetails gameDetails,
    PathDetails pathDetails,
    LauncherDetails launcherDetails,
    ClientDetails clientDetails,
    IBeaconLogger logger,
    Resolution? resolution = null,
    IProgressReporter? progressReporter = null)
    : MinecraftInstance(id, gameVersion, gameDetails, pathDetails, launcherDetails, clientDetails, logger, resolution,
        progressReporter)
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when <see cref="MinecraftInstance.ArgumentBuilder" /> is null.</exception>
    public override async Task<ModdedData?> InstallModdedAsync(string tempDir, IHttpService httpService,
        CancellationToken cancellationToken = default)
    {
        if (ArgumentBuilder == null)
            throw new InvalidOperationException($"{nameof(ArgumentBuilder)} is null.");

        _progressReporter?.Report(EProgressKind.INSTANCE_READING_MANIFEST, 0);
        if (!File.Exists(PathDetails.CustomManifestPath))
        {
            _logger.LogError("Fabric manifest file not found at path: " + PathDetails.CustomManifestPath);
            return null;
        }

        // Create versionDir in the versions folder
        Directory.CreateDirectory(VersionData.CustomVersionDirectory!);

        // Download version json
        FabricVersionMeta? fabricVersionMeta;
        List<LibraryMeta> localLibraries = [];

        if (!File.Exists(VersionData.CustomJsonPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_VERSION_JSON, e);
            };

            var fabricVersionJsonUrl = string.Format(FabricEndpoints.LoaderJsonUrl, VersionData.MinecraftVersion,
                VersionData.CustomVersion);

            var resultJson = await httpService.GetStringAsync(fabricVersionJsonUrl, progress, cancellationToken);
            if (resultJson == null)
                return null;

            await File.WriteAllTextAsync(VersionData.CustomJsonPath!, resultJson, cancellationToken);

            // Add the libraries
            _progressReporter?.Report(EProgressKind.INSTANCE_READING_VERSION_JSON, 0);
            fabricVersionMeta =
                JsonSerializer.Deserialize<FabricVersionMeta>(resultJson, CoreJsonContext.Default.FabricVersionMeta);
            if (fabricVersionMeta == null)
            {
                FileSystemHelper.DeleteFile(VersionData
                    .CustomJsonPath!); // Delete it because this if part won't be executed again if it exists
                _logger.LogError("Fabric version meta is null after deserialization. Invalid JSON format.");
                return null;
            }

            foreach (var lib in fabricVersionMeta.Libraries)
                localLibraries.Add(new LibraryMeta(lib.Name,
                    new LibraryDownloads(new Artifact(lib.GetPath(), lib.Sha1, lib.Size, lib.GetURL()), null), []));
        }
        else
        {
            _progressReporter?.Report(EProgressKind.INSTANCE_READING_VERSION_JSON, 0);
            fabricVersionMeta = await JsonHelper.ReadJsonFileAsync<FabricVersionMeta>(VersionData.CustomJsonPath!,
                CoreJsonContext.Default.FabricVersionMeta, cancellationToken);
            if (fabricVersionMeta == null)
            {
                _logger.LogError("Fabric version meta is null after deserialization. Invalid JSON format.");
                return null;
            }

            foreach (var lib in fabricVersionMeta.Libraries)
                localLibraries.Add(new LibraryMeta(lib.Name,
                    new LibraryDownloads(new Artifact(lib.GetPath(), lib.Sha1, lib.Size, lib.GetURL()), null), []));
        }


        // Download Loader
        var loaderDirPath = Path.Combine(PathDetails.LibrariesDir, "net", "fabricmc", "fabric-loader",
            VersionData.CustomVersion!);
        var loaderJarPath = Path.Combine(loaderDirPath, $"fabric-loader-{VersionData.CustomVersion}.jar");
        Directory.CreateDirectory(loaderDirPath);

        if (!File.Exists(loaderJarPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_FABRIC_LOADER, e);
            };
            _logger.LogDebug("Downloading fabric loader jar...");
            await httpService.DownloadFileAsync(string.Format(FabricEndpoints.LoaderJarUrl, VersionData.CustomVersion),
                loaderJarPath, null, progress, cancellationToken);
        }

        foreach (var arg in fabricVersionMeta.Arguments.GetGameArgs())
            ArgumentBuilder.AddGameArgument(new LaunchArg(arg, 1));

        foreach (var arg in fabricVersionMeta.Arguments.GetJvmArgs())
        {
            // Fixes -DFabricMcEmu arg, without this Fabric does not load and instead the vanilla client will launch
            if (arg.StartsWith("-DFabricMcEmu="))
            {
                ArgumentBuilder.AddJvmArgument(new LaunchArg("-DFabricMcEmu=\"net.minecraft.client.main.Main\"", 1));
                continue;
            }

            ArgumentBuilder.AddJvmArgument(new LaunchArg(arg, 1));
        }

        return new ModdedData(fabricVersionMeta.MainClass, localLibraries);
    }
}
