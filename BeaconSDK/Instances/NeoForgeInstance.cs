using System.IO.Compression;
using Tavstal.BeaconSDK.Helpers.Serialization;
using Tavstal.BeaconSDK.Instances.Forge;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Endpoints.Modding;
using Tavstal.BeaconSDK.Models.Installer;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge.Modern;
using Tavstal.BeaconSDK.Models.MojangApi;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Services.Abstractions;
using CoreJsonContext = Tavstal.BeaconSDK.Models.Json.CoreJsonContext;

namespace Tavstal.BeaconSDK.Instances;

/// <summary>
///     An implementation of <see cref="ForgeInstanceBase" /> that installs and configures NeoForge-based modded instances.
///     NeoForge is the community-driven fork of Minecraft Forge for modern Java Edition versions.
/// </summary>
/// <param name="id">Unique identifier for the instance.</param>
/// <param name="gameVersion">The vanilla <see cref="MinecraftVersion" /> used as the base for this instance.</param>
/// <param name="gameDetails">Details about the game/runtime configuration.</param>
/// <param name="pathDetails">Filesystem paths used by the instance (libraries, assets, etc.).</param>
/// <param name="launcherDetails">Launcher metadata and settings.</param>
/// <param name="clientDetails">Client-specific data (e.g. credentials).</param>
/// <param name="logger">Logger used for diagnostic messages.</param>
/// <param name="resolution">Optional default resolution for the instance.</param>
/// <param name="progressReporter">Optional progress reporter used to surface download/install progress to callers.</param>
public class NeoForgeInstance(
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
        if (ArgumentBuilder == null)
            throw new InvalidOperationException($"{nameof(ArgumentBuilder)} is null.");

        if (!File.Exists(PathDetails.CustomManifestPath))
        {
            _logger.LogError("NeoForge manifest file does not exist. Please ensure the manifest is downloaded.");
            return null;
        }

        List<LibraryMeta> localLibraries = [];

        // Create versionDir in the versions folder
        Directory.CreateDirectory(VersionData.CustomVersionDirectory!);

        // Download & Extract Installer
        var installerJarPath = Path.Combine(tempDir, "installer.jar");
        var installerDir = Path.Combine(tempDir, "installer");
        var installerProfilePath = Path.Combine(VersionData.CustomVersionDirectory!, "install_profile.json");
        if (!File.Exists(VersionData.CustomJsonPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_NEOFORGE_INSTALLER, e);
            };

            await httpService.DownloadFileAsync(
                string.Format(NeoForgeEndpoints.InstallerJarUrl, VersionData.CustomVersion), installerJarPath,
                progress, cancellationToken);

            // Extract Installer
            _progressReporter?.Report(EProgressKind.INSTANCE_EXTRACTING_INSTALLER, 0);
            await ZipFile.ExtractToDirectoryAsync(installerJarPath, installerDir, cancellationToken);

            // Move install_profile.json
            var source = Path.Combine(installerDir, "install_profile.json");
            if (File.Exists(source))
                File.Move(source, installerProfilePath);
            else
                _logger.LogError("Install profile JSON file not found in the neoforge installer directory.");

            // Move version.json
            source = Path.Combine(installerDir, "version.json");
            if (File.Exists(source))
                File.Move(source, VersionData.CustomJsonPath!);
            else
                _logger.LogError("Install version JSON file not found in the neoforge installer directory.");

            // Extract Maven
            source = Path.Combine(installerDir, "maven");
            if (Directory.Exists(source))
            {
                var content = Directory.GetDirectories(source);
                foreach (var dir in content)
                {
                    var newDirPath = dir.Replace(source, PathDetails.LibrariesDir);
                    if (!Directory.Exists(newDirPath))
                        Directory.CreateDirectory(newDirPath);
                }

                content = Directory.GetFiles(source);
                foreach (var file in content)
                {
                    var newFilePath = file.Replace(source, PathDetails.LibrariesDir);
                    if (!File.Exists(newFilePath))
                        File.Copy(file, newFilePath, true);
                }
            }
            else
            {
                _logger.LogWarning("Maven directory not found in the neoforge installer directory.");
            }
        }

        // Read Forge Version Meta
        var forgeVersionMeta = await JsonHelper.ReadJsonFileAsync<ForgeVersionMeta>(VersionData.CustomJsonPath!,
            CoreJsonContext.Default.ForgeVersionMetaModern, cancellationToken);
        if (forgeVersionMeta == null)
            throw new FileNotFoundException("Failed to get the neoforge version meta.");

        // Install libraries from Forge Version Meta
        localLibraries.AddRange(forgeVersionMeta.Libraries);

        // Read Forge Install Profile
        var installProfile = await JsonHelper.ReadJsonFileAsync<ForgeVersionProfile>(installerProfilePath,
            CoreJsonContext.Default.ForgeVersionProfile, cancellationToken);
        if (installProfile == null)
            throw new FileNotFoundException("Failed to get the neoforge install profile meta.");

        // Install Libraries From Install Profile
        _progressReporter?.Report(EProgressKind.INSTANCE_READING_LIBRARIES, 0);
        foreach (var libMeta in installProfile.Libraries)
        {
            if (libMeta.Downloads.Artifact == null)
            {
                _logger.LogWarning($"Library {libMeta.Name} does not have an artifact to download.");
                continue;
            }

            var localPath = libMeta.Downloads.Artifact.Path;
            var libraryDir = Path.Combine(PathDetails.LibrariesDir,
                localPath.Remove(localPath.LastIndexOf('/'), localPath.Length - localPath.LastIndexOf('/')));

            if (!Directory.Exists(libraryDir))
                Directory.CreateDirectory(libraryDir);

            var libraryPath = Path.Combine(PathDetails.LibrariesDir, localPath);
            if (File.Exists(libraryPath))
                continue;

            if (string.IsNullOrEmpty(libMeta.Downloads.Artifact.Url))
            {
                _logger.LogWarning($"Library {libMeta.Name} does not have a download URL.");
                continue;
            }

            var libProgress = new Progress<double>();
            libProgress.ProgressChanged += (_, e) =>
            {
                _progressReporter?.Report(e, libMeta.Name);
            };

            await httpService.DownloadFileAsync(libMeta.Downloads.Artifact.Url, libraryPath, libProgress,
                cancellationToken);
        }

        // Map and start processors
        _progressReporter?.Report(EProgressKind.INSTANCE_BUILDING_NEOFORGE, 0);
        if (File.Exists(installerJarPath))
            await MapAndStartProcessors(installProfile, installerDir);

        // Copy Version Files
        var jarSourcePath = Path.Combine(installerDir, "maven", "net", "neoforged", "neoforge",
            $"{VersionData.MinecraftVersion}-{VersionData.CustomVersion}",
            $"neoforge-{VersionData.CustomVersion}-universal.jar");
        _logger.LogDebug("Source jar path: " + jarSourcePath);
        if (File.Exists(jarSourcePath))
        {
            var targetJarPath = Path.Combine(VersionData.CustomVersionDirectory!,
                $"neoforge-{VersionData.MinecraftVersion}-{VersionData.CustomVersion}.jar");
            File.Copy(jarSourcePath, targetJarPath);
        }

        // Add launch arguments
        _progressReporter?.Report(EProgressKind.INSTANCE_BUILDING_ARGUMENTS, 0);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (forgeVersionMeta.Arguments != null)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (forgeVersionMeta.Arguments.Game != null)
                foreach (var arg in forgeVersionMeta.Arguments.GetGameArgs())
                    ArgumentBuilder.AddGameArgument(new LaunchArg(arg, 1));

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (forgeVersionMeta.Arguments.Jvm != null)
                foreach (var arg in forgeVersionMeta.Arguments.GetJvmArgs())
                    ArgumentBuilder.AddJvmArgument(new LaunchArg(arg, 1));
        }

        return new ModdedData(forgeVersionMeta.MainClass, localLibraries);
    }
}
