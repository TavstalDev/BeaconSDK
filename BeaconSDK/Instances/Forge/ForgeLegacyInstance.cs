using System.IO.Compression;
using Tavstal.BeaconSDK.Helpers.Serialization;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Endpoints.Modding;
using Tavstal.BeaconSDK.Models.Installer;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge.Legacy;
using Tavstal.BeaconSDK.Models.MojangApi;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;
using Tavstal.BeaconSDK.Services.Abstractions;
using CoreJsonContext = Tavstal.BeaconSDK.Models.Json.CoreJsonContext;

namespace Tavstal.BeaconSDK.Instances.Forge;

/// <summary>
///     Represents a legacy Forge instance implementation for Minecraft versions 1.7.10 through 1.12.1.
///     Handles installer/profile-based setup logic specific to this Forge generation.
/// </summary>
/// <param name="forgeVersionName">
///     The Forge version identifier used for resolving installer and universal artifacts
///     (for example: <c>1.12.2-14.23.5.2860</c>).
/// </param>
/// <param name="id">Unique identifier for this launcher instance.</param>
/// <param name="gameVersion">The base Minecraft version metadata selected for the instance.</param>
/// <param name="gameDetails">Game-specific runtime settings (kind, version values, custom paths, etc.).</param>
/// <param name="pathDetails">Filesystem path configuration used by the instance (versions, libraries, assets, ...).</param>
/// <param name="launcherDetails">Launcher-level configuration and metadata.</param>
/// <param name="clientDetails">Client/session metadata associated with this instance.</param>
/// <param name="logger">Logger used for diagnostics, warnings, and install progress messages.</param>
/// <param name="resolution">Optional launch resolution override.</param>
/// <param name="progressReporter">Optional progress reporter for long-running install/download operations.</param>
public class ForgeLegacyInstance(
    string forgeVersionName,
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

        // Download Installer
        var installerJarPath = Path.Combine(tempDir, "installer.jar");
        var installerDir = Path.Combine(tempDir, $"installer-{forgeVersionName}");
        var installerProfilePath = Path.Combine(VersionData.CustomVersionDirectory!, "install_profile.json");
        var forgeUniversalDir = Path.Combine(PathDetails.LibrariesDir, "net", "minecraftforge", "forge",
            forgeVersionName);
        var forgeUniversalPath = Path.Combine(forgeUniversalDir,
            $"forge-{forgeVersionName}-universal.jar");
        if (!File.Exists(VersionData.CustomJsonPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_FORGE_INSTALLER, e);
            };

            await httpService.DownloadFileAsync(
                string.Format(ForgeEndpoints.InstallerJarUrl, forgeVersionName), installerJarPath,
                progress, cancellationToken);

            // Extract Installer
            _progressReporter?.Report(EProgressKind.INSTANCE_EXTRACTING_INSTALLER, 0);
            await ZipFile.ExtractToDirectoryAsync(installerJarPath, installerDir, cancellationToken);

            // Move install_profile.json
            var source = Path.Combine(installerDir, "install_profile.json");
            if (File.Exists(source))
                File.Move(source, installerProfilePath, true);
            else
                _logger.LogError("Install profile JSON file not found in the forge installer directory.");

            // Extract universal jar
            var universalJarPath = Path.Combine(installerDir, $"forge-{forgeVersionName}-universal.jar");
            _logger.LogDebug("Checking for universal jar at: " + universalJarPath);
            if (File.Exists(universalJarPath))
            {
                var universalDir = Path.Combine(tempDir,
                    $"{forgeVersionName}-universal");
                if (!Directory.Exists(universalDir))
                    await ZipFile.ExtractToDirectoryAsync(universalJarPath, universalDir, cancellationToken);

                // COPY UNIVERSAL
                if (!Directory.Exists(forgeUniversalDir))
                    Directory.CreateDirectory(forgeUniversalDir);

                if (!File.Exists(forgeUniversalPath))
                    File.Copy(universalJarPath, forgeUniversalPath, true);

                // VERSION
                if (!File.Exists(VersionData.CustomJsonPath))
                    File.Move(Path.Combine(universalDir, "version.json"), VersionData.CustomJsonPath!, true);
            }
            else
            {
                _logger.LogWarning(
                    "Forge universal jar not found in the installer directory. This may indicate an issue with the installer.");
            }

            // Maven directory does not exist in this version
        }

        // Add Forge Universal Jar to classpath
        ArgumentBuilder.AddClass(forgeUniversalPath);

        // Read Forge Version Meta
        var forgeVersionMeta = await JsonHelper.ReadJsonFileAsync<ForgeVersionMeta>(VersionData.CustomJsonPath!,
            CoreJsonContext.Default.ForgeVersionMetaLegacy, cancellationToken);
        if (forgeVersionMeta == null)
            throw new FileNotFoundException("Failed to get the forge version meta.");

        // Install libraries from Forge Version Meta
        foreach (var lib in forgeVersionMeta.Libraries)
        {
            var url = lib.GetUrl(true);
            if (url == null)
                continue;

            localLibraries.Add(new LibraryMeta
            {
                Name = lib.Name,
                Downloads = new LibraryDownloads
                {
                    Artifact = new Artifact { Path = lib.GetPath(), Sha1 = string.Empty, Size = 0, Url = url },
                    Classifiers = null
                },
                Natives = null,
                Rules = []
            });
        }

        // Read Forge Install Profile
        var installProfile = await JsonHelper.ReadJsonFileAsync<ForgeProfile>(installerProfilePath,
            CoreJsonContext.Default.ForgeProfile, cancellationToken);
        if (installProfile == null)
            throw new FileNotFoundException("Failed to get the forge install profile meta.");

        _progressReporter?.Report(EProgressKind.INSTANCE_BUILDING_ARGUMENTS, 0);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (forgeVersionMeta.MinecraftArguments != null)
        {
            MinecraftVersionMeta.ArgumentsLegacy = forgeVersionMeta.MinecraftArguments;
            var args = forgeVersionMeta.MinecraftArguments.Split(' ');
            var tweakIndex = args.IndexOf("--tweakClass");
            ArgumentBuilder.AddGameArgument(new LaunchArg($"--tweakClass {args[tweakIndex + 1]}", 1));
        }

        // Copy vanilla jar
        if (!File.Exists(VersionData.CustomJarPath))
            //ReportProgress(0, $"ui_copying_jar", "vanilla");
            File.Copy(VersionData.VanillaJarPath, VersionData.CustomJarPath!, true);
        return new ModdedData(forgeVersionMeta.MainClass, localLibraries);
    }
}
