using System.IO.Compression;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Endpoints.Modding;
using Tavstal.BeaconSDK.Models.Installer;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.MojangApi;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Instances.Forge;

// 1.1 - 1.5.2

/// <summary>
///     Instance implementation for legacy Forge versions (approximately 1.1 - 1.5.2).
///     This class handles the legacy installation flow where a "universal" zip is applied
///     to a vanilla jar to produce a patched (forge-enabled) jar, and legacy libraries are staged.
/// </summary>
/// <param name="forgeVersionName">The Forge version identifier used to build download URLs (e.g. "10.13.4.1614").</param>
/// <param name="universalName">The name of the universal artifact inside the Forge distribution (used in download URL).</param>
/// <param name="id">Unique identifier for the instance.</param>
/// <param name="gameVersion">The base vanilla <see cref="MinecraftVersion" /> used by this instance.</param>
/// <param name="gameDetails">Game-level configuration (version strings, custom game directory, etc.).</param>
/// <param name="pathDetails">Filesystem path configuration for versions, libraries, assets and so on.</param>
/// <param name="launcherDetails">Launcher-level metadata.</param>
/// <param name="clientDetails">Client-specific details such as auth tokens or client-side settings.</param>
/// <param name="logger">Logger used for diagnostics and progress reporting.</param>
/// <param name="resolution">Optional default resolution for the instance window.</param>
/// <param name="progressReporter">Optional progress reporter to surface download/extract progress to callers.</param>
public class ForgeEarlyInstance(
    string forgeVersionName,
    string universalName,
    string id,
    MinecraftVersion gameVersion,
    GameDetails gameDetails,
    PathDetails pathDetails,
    LauncherDetails launcherDetails,
    ClientDetails clientDetails,
    ICustomLogger logger,
    Resolution? resolution = null,
    IProgressReporter? progressReporter = null)
    : ForgeInstanceBase(id, gameVersion, gameDetails, pathDetails, launcherDetails, clientDetails, logger, resolution,
        progressReporter)
{
    /// <inheritdoc />
    public override async Task<ModdedData?> InstallModdedAsync(string tempDir, IHttpService httpService,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(PathDetails.CustomManifestPath))
        {
            _logger.LogError("Forge manifest file does not exist. Please ensure the manifest is downloaded.");
            return null;
        }

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
                string.Format(ForgeEndpoints.LoaderUniversalZipUrl, forgeVersionName, universalName), universalZipPath,
                progress, cancellationToken);

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
        var libraries = ForgeInstance.GetLegacyLibraries(GameDetails.MinecraftVersion);
        if (libraries.Count == 0)
            return moddedData;

        var forgeLibDir = Path.Combine(VersionData.GameDir, "lib");
        if (!Directory.Exists(forgeLibDir))
            Directory.CreateDirectory(forgeLibDir);

        // Copy legacy libraries to the forge lib directory
        foreach (var library in libraries)
        {
            var libraryFileName = library.Replace("Tavstal.BeaconSDK.Assets.Fmllib.", "");
            var libraryTargetPath = Path.Combine(forgeLibDir, libraryFileName);

            if (File.Exists(libraryTargetPath))
                continue;

            var stream = GetType().Assembly.GetManifestResourceStream(library);
            if (stream == null)
            {
                _logger.LogError($"Failed to get resource stream for {library}");
                continue;
            }

            await using var outFile = new FileStream(libraryTargetPath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(outFile, cancellationToken);
        }

        return moddedData;
    }
}
