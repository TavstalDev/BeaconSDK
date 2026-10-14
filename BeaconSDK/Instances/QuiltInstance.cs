using System.Text.Json;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Helpers.Serialization;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Instance.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.ModLoaders.Fabric;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;
using Tavstal.BeaconSDK.Services.Abstractions;
using CoreJsonContext = Tavstal.BeaconSDK.Models.Json.CoreJsonContext;
// ReSharper disable UnusedType.Global

namespace Tavstal.BeaconSDK.Instances;

/// <summary>
///     An implementation of <see cref="MinecraftInstance" /> that installs and configures Quilt-based modded instances.
/// </summary>
/// <param name="id">Unique identifier for the instance.</param>
/// <param name="appConfiguration">The Beacon SDK configuration providing endpoint, HTTP, authentication and data protection settings.</param>
/// <param name="configuration">The configuration describing the game, paths, launcher and client settings for the instance.</param>
/// <param name="logger">Logger used for diagnostic messages.</param>
/// <param name="progressReporter">Optional progress reporter used to surface download/install progress to callers.</param>
public class QuiltInstance(
    string id,
    BeaconConfiguration appConfiguration,
    InstanceConfiguration configuration,
    IBeaconLogger logger,
    IProgressReporter? progressReporter = null)
    : MinecraftInstance(id, appConfiguration, configuration, logger, progressReporter)
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when <see cref="MinecraftInstance.ArgumentBuilder" /> is null.</exception>
    public override async Task<ModdedData?> InstallModdedAsync(string tempDir, IHttpService httpService,
        CancellationToken cancellationToken = default)
    {
        if (ArgumentBuilder == null)
            throw new InvalidOperationException($"{nameof(ArgumentBuilder)} is null.");

        _progressReporter?.Report(EProgressKind.INSTANCE_READING_MANIFEST, 0);

        // Create versionDir in the versions folder
        Directory.CreateDirectory(VersionData.CustomVersionDirectory!);

        // Download version json
        FabricVersionMeta? quiltVersionMeta;
        List<LibraryMeta> localLibraries = [];

        if (!File.Exists(VersionData.CustomJsonPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_VERSION_JSON, e);
            };

            var quiltVersionJsonUrl = string.Format(_appConfiguration.Endpoints.Quilt.LoaderJsonUrl, VersionData.MinecraftVersion,
                VersionData.CustomVersion);

            var resultJson = await httpService.GetStringAsync(quiltVersionJsonUrl, progress, cancellationToken);
            if (resultJson == null)
                return null;

            await File.WriteAllTextAsync(VersionData.CustomJsonPath!, resultJson, cancellationToken);

            // Add the libraries
            _progressReporter?.Report(EProgressKind.INSTANCE_READING_VERSION_JSON, 0);
            quiltVersionMeta =
                JsonSerializer.Deserialize<FabricVersionMeta>(resultJson, CoreJsonContext.Default.FabricVersionMeta);
            if (quiltVersionMeta == null)
            {
                FileSystemHelper.DeleteFile(VersionData
                    .CustomJsonPath!); // Delete it because this part won't be executed again if it exists
                _logger.LogError("Quilt version meta is null after deserialization. Invalid JSON format.");
                return null;
            }

            foreach (var lib in quiltVersionMeta.Libraries)
                localLibraries.Add(new LibraryMeta(lib.Name,
                    new LibraryDownloads(new Artifact(lib.GetPath(), lib.Sha1, lib.Size, lib.GetURL()), null), []));
        }
        else
        {
            _progressReporter?.Report(EProgressKind.INSTANCE_READING_VERSION_JSON, 0);
            quiltVersionMeta = await JsonHelper.ReadJsonFileAsync<FabricVersionMeta>(VersionData.CustomJsonPath!,
                CoreJsonContext.Default.FabricVersionMeta, cancellationToken);
            if (quiltVersionMeta == null)
            {
                _logger.LogError("Quilt version meta is null after deserialization. Invalid JSON format.");
                return null;
            }

            foreach (var lib in quiltVersionMeta.Libraries)
                localLibraries.Add(new LibraryMeta(lib.Name,
                    new LibraryDownloads(new Artifact(lib.GetPath(), lib.Sha1, lib.Size, lib.GetURL()), null), []));
        }


        // Download Loader
        var loaderDirPath = Path.Combine(Config.Path.LibrariesDir, "net", "quiltmc", "quilt-loader",
            VersionData.CustomVersion!);
        var loaderJarPath = Path.Combine(loaderDirPath, $"quilt-loader-{VersionData.CustomVersion}.jar");
        Directory.CreateDirectory(loaderDirPath);

        if (!File.Exists(loaderJarPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_QUILT_LOADER, e);
            };
            _logger.LogDebug("Downloading quilt loader jar...");
            await httpService.DownloadFileAsync(string.Format(_appConfiguration.Endpoints.Quilt.LoaderJarUrl, VersionData.CustomVersion),
                loaderJarPath, null, progress, cancellationToken);
        }


        foreach (var arg in quiltVersionMeta.Arguments.GetGameArgs())
            ArgumentBuilder.AddGameArgument(new LaunchArg(arg, 1));

        foreach (var arg in quiltVersionMeta.Arguments.GetJvmArgs())
        {
            // Fixes -DFabricMcEmu arg, without this Quilt does not load and instead the vanilla client will launch
            if (arg.StartsWith("-DFabricMcEmu="))
            {
                ArgumentBuilder.AddJvmArgument(new LaunchArg("-DFabricMcEmu=\"net.minecraft.client.main.Main\"", 1));
                continue;
            }

            ArgumentBuilder.AddJvmArgument(new LaunchArg(arg, 1));
        }

        return new ModdedData(quiltVersionMeta.MainClass, localLibraries);
    }
}
