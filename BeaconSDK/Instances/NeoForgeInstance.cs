using System.IO.Compression;
using Tavstal.BeaconSDK.Helpers.Serialization;
using Tavstal.BeaconSDK.Instances.Forge;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Instance.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge.Modern;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Services.Abstractions;
using CoreJsonContext = Tavstal.BeaconSDK.Models.Json.CoreJsonContext;
// ReSharper disable UnusedType.Global

namespace Tavstal.BeaconSDK.Instances;

/// <summary>
///     An implementation of <see cref="ForgeInstanceBase" /> that installs and configures NeoForge-based modded instances.
///     NeoForge is the community-driven fork of Minecraft Forge for modern Java Edition versions.
/// </summary>
/// <param name="id">Unique identifier for the instance.</param>
/// <param name="appConfiguration">The Beacon SDK configuration providing endpoint, HTTP, authentication and data protection settings.</param>
/// <param name="configuration">The configuration describing the game, paths, launcher and client settings for the instance.</param>
/// <param name="logger">Logger used for diagnostic messages.</param>
/// <param name="progressReporter">Optional progress reporter used to surface download/install progress to callers.</param>
public class NeoForgeInstance(
    string id,
    BeaconConfiguration appConfiguration,
    InstanceConfiguration configuration,
    IBeaconLogger logger,
    IProgressReporter? progressReporter = null)
    : ForgeInstanceBase(id, appConfiguration, configuration, logger, progressReporter)
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when <see cref="MinecraftInstance.ArgumentBuilder" /> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the NeoForge version or install profile metadata cannot be read.</exception>
    public override async Task<ModdedData?> InstallModdedAsync(string tempDir, IHttpService httpService,
        CancellationToken cancellationToken = default)
    {
        if (ArgumentBuilder == null)
            throw new InvalidOperationException($"{nameof(ArgumentBuilder)} is null.");

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
                string.Format(_appConfiguration.Endpoints.NeoForge.InstallerJarUrl, VersionData.CustomVersion), installerJarPath,
                null, progress, cancellationToken);

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
                    var newDirPath = dir.Replace(source, Config.Path.LibrariesDir);
                    if (!Directory.Exists(newDirPath))
                        Directory.CreateDirectory(newDirPath);
                }

                content = Directory.GetFiles(source);
                foreach (var file in content)
                {
                    var newFilePath = file.Replace(source, Config.Path.LibrariesDir);
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
            var libraryDir = Path.Combine(Config.Path.LibrariesDir,
                localPath.Remove(localPath.LastIndexOf('/'), localPath.Length - localPath.LastIndexOf('/')));

            if (!Directory.Exists(libraryDir))
                Directory.CreateDirectory(libraryDir);

            var libraryPath = Path.Combine(Config.Path.LibrariesDir, localPath);
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

            await httpService.DownloadFileAsync(libMeta.Downloads.Artifact.Url, libraryPath,
                libMeta.Downloads.Artifact.Sha1, libProgress, cancellationToken);
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
