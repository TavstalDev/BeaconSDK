using System.IO.Compression;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Helpers.Serialization;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Endpoints.Modding;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge.Legacy;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;
using Tavstal.BeaconSDK.Services.Abstractions;
using CoreJsonContext = Tavstal.BeaconSDK.Models.Json.CoreJsonContext;

namespace Tavstal.BeaconSDK.Instances.Forge;

/// <summary>
///     Represents a classic Forge instance installer/runner implementation for legacy Minecraft versions
///     in the 1.5.2 to 1.7.2 range.
/// </summary>
/// <param name="forgeVersionName">
///     The full Forge version identifier used for resolving installer/universal artifacts
///     (for example, values like "1.6.4-9.11.1.1345").
/// </param>
/// <param name="universalFormat">The expected universal jar naming format used by this Forge generation.</param>
/// <param name="id">Unique identifier for the launcher instance.</param>
/// <param name="configuration">The configuration describing the game, paths, launcher and client settings for the instance.</param>
/// <param name="logger">Logger used for progress, diagnostics, and error reporting.</param>
/// <param name="progressReporter">Optional progress reporter used to surface long-running install status updates.</param>
public class ForgeClassicInstance(
    string forgeVersionName,
    string universalFormat,
    string id,
    InstanceConfiguration configuration,
    IBeaconLogger logger,
    IProgressReporter? progressReporter = null)
    : ForgeInstanceBase(id, configuration, logger, progressReporter)
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when <see cref="MinecraftInstance.ArgumentBuilder" /> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the Forge version or install profile metadata cannot be read.</exception>
    public override async Task<ModdedData?> InstallModdedAsync(string tempDir, IHttpService httpService,
        CancellationToken cancellationToken = default)
    {
        if (ArgumentBuilder == null)
            throw new InvalidOperationException($"{nameof(ArgumentBuilder)} is null.");

        if (!File.Exists(Config.Path.CustomManifestPath))
        {
            _logger.LogError("Forge manifest file does not exist. Please ensure the manifest is downloaded.");
            return null;
        }

        List<LibraryMeta> localLibraries = [];

        // Create versionDir in the versions folder
        Directory.CreateDirectory(VersionData.CustomVersionDirectory!);

        // Download Installer
        var universalFormatName = universalFormat.Replace("${version}", forgeVersionName);
        var installerJarPath = Path.Combine(tempDir, "installer.jar");
        var installerDir = Path.Combine(tempDir, $"installer-{forgeVersionName}");
        var installerProfilePath = Path.Combine(VersionData.CustomVersionDirectory!, "install_profile.json");
        var forgeUniversalDir = Path.Combine(Config.Path.LibrariesDir, "net", "minecraftforge", "forge",
            forgeVersionName);
        var forgeUniversalPath = Path.Combine(forgeUniversalDir,
            universalFormatName);

        if (!File.Exists(VersionData.CustomJarPath))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                _progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_FORGE_INSTALLER, e);
            };

            await httpService.DownloadFileAsync(
                string.Format(ForgeEndpoints.InstallerJarUrl, forgeVersionName), installerJarPath,
                null, progress, cancellationToken);

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
            var universalJarPath = Path.Combine(installerDir, universalFormatName);
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
                var versionJsonPath = Path.Combine(universalDir, "version.json");
                if (!File.Exists(VersionData.CustomJsonPath) && File.Exists(versionJsonPath))
                    File.Move(versionJsonPath, VersionData.CustomJsonPath!, true);
            }
            else
            {
                _logger.LogWarning(
                    "Forge universal jar not found in the installer directory. This may indicate an issue with the installer.");
            }

            // Maven directory does not exist in this version
        }

        // Include Forge Universal Jar Classpath
        ArgumentBuilder.AddClass(forgeUniversalPath);

        // Read Forge Install Profile
        var installProfile =
            await JsonHelper.ReadJsonFileAsync<ForgeProfile>(installerProfilePath,
                CoreJsonContext.Default.ForgeProfile, cancellationToken);
        if (installProfile == null)
            throw new FileNotFoundException("Failed to get the forge install profile meta.");

        // Fix 1.6.1 Forge Version
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Config.Game.MinecraftVersion == "1.6.1" && !File.Exists(VersionData.CustomJsonPath) &&
            installProfile.VersionInfo != null)
            await JsonHelper.WriteJsonFileAsync(VersionData.CustomJsonPath!, installProfile.VersionInfo,
                CoreJsonContext.Default.ForgeVersionMetaLegacy, cancellationToken);

        // Read Forge Version Meta
        string? mainClass = null;
        if (File.Exists(VersionData.CustomJsonPath))
        {
            var forgeVersionMeta = await JsonHelper.ReadJsonFileAsync<ForgeVersionMeta>(VersionData.CustomJsonPath,
                CoreJsonContext.Default.ForgeVersionMetaLegacy, cancellationToken);
            if (forgeVersionMeta == null)
                throw new FileNotFoundException("Failed to get the forge version meta.");
            mainClass = forgeVersionMeta.MainClass;

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
                        Artifact = new Artifact
                        {
                            Path = lib.GetPath(), Sha1 = string.Empty, Size = 0, Url = url
                        },
                        Classifiers = null
                    },
                    Natives = null,
                    Rules = []
                });
            }

            _progressReporter?.Report(EProgressKind.INSTANCE_BUILDING_ARGUMENTS, 0);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (forgeVersionMeta.MinecraftArguments != null)
                MinecraftVersionMeta.ArgumentsLegacy = forgeVersionMeta.MinecraftArguments;
        }

        // Patch vanilla jar
        // Fixes java 8 compatibility issues with Forge 1.5.2 - 1.7.2
        if (mainClass != null)
        {
            if (!File.Exists(VersionData.CustomJarPath))
                //ReportProgress(0, $"ui_copying_jar", "vanilla");
                File.Copy(VersionData.VanillaJarPath, VersionData.CustomJarPath!);
        }
        else
        {
            if (!File.Exists(VersionData.CustomJarPath))
            {
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
                await ZipFile.ExtractToDirectoryAsync(forgeUniversalPath, vanillaExtractDir, true, cancellationToken);
                var patchedVanillaJarPath = Path.Combine(tempDir, "patched_vanilla.jar");
                await ZipFile.CreateFromDirectoryAsync(vanillaExtractDir, patchedVanillaJarPath, cancellationToken);

                File.Copy(patchedVanillaJarPath, VersionData.CustomJarPath!);
            }
        }

        var legacyLibraries = ForgeInstance.GetLegacyLibraries(Config.Game.MinecraftVersion);
        var moddedData = new ModdedData(mainClass, legacyLibraries.Count > 0 ? [] : localLibraries);
        if (legacyLibraries.Count == 0)
            return moddedData;

        var forgeLibDir = Path.Combine(VersionData.GameDir, "lib");
        if (!Directory.Exists(forgeLibDir))
            Directory.CreateDirectory(forgeLibDir);

        // Copy legacy libraries to the forge lib directory
        foreach (var library in legacyLibraries)
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
