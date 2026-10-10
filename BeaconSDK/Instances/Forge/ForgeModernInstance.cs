using System.IO.Compression;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Helpers.Serialization;
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

namespace Tavstal.BeaconSDK.Instances.Forge;

/// <summary>
///     Represents a modern Forge instance implementation for Minecraft 1.12.2 and newer.
///     This type extends <see cref="ForgeInstanceBase" /> and is responsible for installer/profile-based
///     setup logic used by newer Forge distributions.
/// </summary>
/// <param name="id">Unique identifier for the launcher instance.</param>
/// <param name="gameVersion">The base Mojang <see cref="MinecraftVersion" /> selected for this instance.</param>
/// <param name="gameDetails">Game-level settings such as Minecraft version, Forge version, and custom directories.</param>
/// <param name="pathDetails">Resolved filesystem locations (versions, libraries, assets, cache, etc.).</param>
/// <param name="launcherDetails">Launcher metadata and configuration used during installation and launch preparation.</param>
/// <param name="clientDetails">Client/session information associated with this instance.</param>
/// <param name="logger">Logger used to report installation progress, warnings, and errors.</param>
/// <param name="resolution">Optional launch resolution override for the game window.</param>
/// <param name="progressReporter">Optional progress reporter for UI/status updates during long-running tasks.</param>
public class ForgeModernInstance(
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
            _logger.LogError("Forge manifest file does not exist. Please ensure the manifest is downloaded.");
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
                _progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_FORGE_INSTALLER, e);
            };

            await httpService.DownloadFileAsync(
                string.Format(ForgeEndpoints.InstallerJarUrl,
                    $"{VersionData.MinecraftVersion}-{VersionData.CustomVersion}"), installerJarPath,
                progress, cancellationToken);

            // Extract Installer
            _progressReporter?.Report(EProgressKind.INSTANCE_EXTRACTING_INSTALLER, 0);
            await ZipFile.ExtractToDirectoryAsync(installerJarPath, installerDir, cancellationToken);

            // Move install_profile.json
            var source = Path.Combine(installerDir, "install_profile.json");
            if (File.Exists(source))
                File.Move(source, installerProfilePath);
            else
                _logger.LogError("Install profile JSON file not found in the forge installer directory.");

            // Move version.json
            source = Path.Combine(installerDir, "version.json");
            if (File.Exists(source))
                File.Move(source, VersionData.CustomJsonPath!);
            else
                _logger.LogError("Install version JSON file not found in the forge installer directory.");

            // Extract Maven
            source = Path.Combine(installerDir, "maven");
            if (Directory.Exists(source))
                FileSystemHelper.MoveDirectory(source, PathDetails.LibrariesDir, true, false);
        }

        // Read Forge Version Meta
        var forgeVersionMeta = await JsonHelper.ReadJsonFileAsync<ForgeVersionMeta>(VersionData.CustomJsonPath!,
            CoreJsonContext.Default.ForgeVersionMetaModern, cancellationToken);
        if (forgeVersionMeta == null)
            throw new FileNotFoundException("Failed to get the forge version meta.");

        // Install libraries from Forge Version Meta
        localLibraries.AddRange(forgeVersionMeta.Libraries);

        // Read Forge Install Profile
        var installProfile = await JsonHelper.ReadJsonFileAsync<ForgeVersionProfile>(installerProfilePath,
            CoreJsonContext.Default.ForgeVersionProfile, cancellationToken);
        if (installProfile == null)
            throw new FileNotFoundException("Failed to get the forge install profile meta.");

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
                _progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_LIBRARIES, e);
            };

            await httpService.DownloadFileAsync(libMeta.Downloads.Artifact.Url, libraryPath, libProgress,
                cancellationToken);
        }

        // Map and start processors
        _progressReporter?.Report(EProgressKind.INSTANCE_BUILDING_FORGE, 0);
        if (File.Exists(installerJarPath))
            await MapAndStartProcessors(installProfile, installerDir);

        // Copy Vanilla Jar
        if (!File.Exists(VersionData.CustomJarPath))
            File.Copy(VersionData.VanillaJarPath, VersionData.CustomJarPath!, true);

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

        if (!string.IsNullOrEmpty(forgeVersionMeta.MinecraftArguments))
            // Only 1.12.2 has this field, and
            // it contains many duplicate vanilla arguments
            // so this is the easiest way to handle it
            ArgumentBuilder.AddGameArgument(
                new LaunchArg("--tweakClass net.minecraftforge.fml.common.launcher.FMLTweaker", 1));

        ArgumentBuilder.AddJvmArgumentBeforeClassPath(
            new LaunchArg($"-DlibraryDirectory={PathDetails.LibrariesDir}", 1));

        return new ModdedData(forgeVersionMeta.MainClass, localLibraries);
    }
}
