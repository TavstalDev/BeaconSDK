using System.IO.Compression;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Instances.Forge;

// 1.1 - 1.5.2

/// <summary>
///     Instance implementation for legacy Forge versions (approximately 1.1 - 1.5.2).
///     This class handles the legacy installation flow where a "universal" zip is applied
///     to a vanilla jar to produce a patched (forge-enabled) jar, and missing legacy libraries are downloaded.
/// </summary>
/// <param name="forgeVersionName">The Forge version identifier used to build download URLs (e.g. "10.13.4.1614").</param>
/// <param name="universalName">The name of the universal artifact inside the Forge distribution (used in download URL).</param>
/// <param name="id">Unique identifier for the instance.</param>
/// <param name="appConfiguration">The Beacon SDK configuration providing endpoint, HTTP, authentication and data protection settings.</param>
/// <param name="configuration">The configuration describing the game, paths, launcher and client settings for the instance.</param>
/// <param name="logger">Logger used for diagnostics and progress reporting.</param>
/// <param name="progressReporter">Optional progress reporter to surface download/extract progress to callers.</param>
public class ForgeEarlyInstance(
    string forgeVersionName,
    string universalName,
    string id,
    BeaconConfiguration appConfiguration,
    InstanceConfiguration configuration,
    IBeaconLogger logger,
    IProgressReporter? progressReporter = null)
    : ForgeInstanceBase(id, appConfiguration, configuration, logger, progressReporter)
{
    /// <inheritdoc />
    public override async Task<ModdedData?> InstallModdedAsync(string tempDir, IHttpService httpService,
        CancellationToken cancellationToken = default)
    {
        // Create versionDir in the versions folder
        Directory.CreateDirectory(VersionData.CustomVersionDirectory!);

        // Download Universal Zip
        var universalZipPath = Path.Combine(tempDir, "universal.zip");
        //string universalExtractDir = Path.Combine(tempDir, $"universal-{forgeVersionName}");
        if (!File.Exists(VersionData.CustomJsonPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_FORGE_INSTALLER, e);
            };

            await httpService.DownloadFileAsync(
                string.Format(_appConfiguration.Endpoints.Forge.LoaderUniversalZipUrl, forgeVersionName, universalName), universalZipPath,
                null, progress, cancellationToken);

            // Create vanilla extract directory
            var vanillaExtractDir = Path.Combine(tempDir, "vanilla_extract");
            if (!Directory.Exists(vanillaExtractDir))
                Directory.CreateDirectory(vanillaExtractDir);

            // Extract vanilla jar
            await ZipFile.ExtractToDirectoryAsync(VersionData.VanillaJarPath, vanillaExtractDir, cancellationToken);
            var vanillaMetaDir = Path.Combine(vanillaExtractDir, "META-INF");
            if (Directory.Exists(vanillaMetaDir))
                FileSystemHelper.DeleteDirectory(vanillaMetaDir);
            else
                _logger.LogWarning(
                    "META-INF directory not found in the vanilla jar. This may indicate an issue with the jar file.");

            // Extract universal jar
            await ZipFile.ExtractToDirectoryAsync(universalZipPath, vanillaExtractDir, true, cancellationToken);
            var patchedVanillaJarPath = Path.Combine(tempDir, "patched_vanilla.jar");
            await ZipFile.CreateFromDirectoryAsync(vanillaExtractDir, patchedVanillaJarPath, cancellationToken);

            File.Copy(patchedVanillaJarPath, VersionData.CustomJarPath!, true);
        }

        var moddedData = new ModdedData(null, []);
        var libraries = ForgeInstance.GetLegacyLibraries(Config.Game.MinecraftVersion);
        if (libraries.Count == 0)
            return moddedData;

        var forgeLibDir = Path.Combine(VersionData.GameDir, "lib");
        if (!Directory.Exists(forgeLibDir))
            Directory.CreateDirectory(forgeLibDir);

        // Download missing legacy libraries to the forge lib directory
        var downloadEntires = new List<DownloadEntry>();
        foreach (var library in libraries)
        {
            var libraryTargetPath = Path.Combine(forgeLibDir, library);

            if (File.Exists(libraryTargetPath))
                continue;

            if (!_appConfiguration.Libraries.Forge.TryGetValue(library, out var libraryEntry) ||
                string.IsNullOrEmpty(libraryEntry.Url))
            {
                _logger.LogWarning($"The '{library}' library URL is missing in the configuration file.");
                continue;
            }

            downloadEntires.Add(new DownloadEntry(libraryEntry.Url, libraryTargetPath, libraryEntry.Sha1));
        }
        await httpService.ParallelDownloadFilesAsync(downloadEntires, cancellationToken);
        return moddedData;
    }
}
