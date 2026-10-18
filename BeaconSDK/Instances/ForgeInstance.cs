using Tavstal.BeaconSDK.Instances.Forge;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Instances;

/// <summary>
///     Factory and helpers for creating Forge-specific <see cref="MinecraftInstance" /> implementations.
/// </summary>
public static class ForgeInstance
{
    /// <summary>
    ///     Create a configured <see cref="MinecraftInstance" /> for the given Forge build.
    /// </summary>
    /// <param name="id">Unique instance identifier.</param>
    /// <param name="appConfiguration">The Beacon SDK configuration providing endpoint, HTTP, authentication and data protection settings.</param>
    /// <param name="configuration">The configuration describing the game, paths, launcher and client settings for the instance.</param>
    /// <param name="logger">Logger for recording instance operations and errors.</param>
    /// <param name="progressReporter">Optional progress reporter used during instance creation/install operations.</param>
    /// <returns>
    ///     A concrete <see cref="MinecraftInstance" /> appropriate for the combination of Minecraft and Forge versions.
    ///     The returned instance will be one of:
    ///     <br />- <see cref="ForgeEarlyInstance" /> for very old (pre-1.3/1.5 era) builds,
    ///     <br />- <see cref="ForgeClassicInstance" /> for older classic builds (1.5 - 1.9 era),
    ///     <br />- <see cref="ForgeLegacyInstance" /> for legacy 1.6 - 1.12 era builds,
    ///     <br />- <see cref="ForgeModernInstance" /> for modern Forge versions (1.12.2+).
    /// </returns>
    public static MinecraftInstance GetForgeInstance(
        string id,
        BeaconConfiguration appConfiguration,
        InstanceConfiguration configuration,
        IBeaconLogger logger,
        IProgressReporter? progressReporter = null)
    {
        var minecraftVersion = new Version(configuration.Game.MinecraftVersion);

        var mcVer = configuration.Game.MinecraftVersion;
        var forgeVer = configuration.Game.CustomVersion!;
        return (minecraftVersion.Major, minecraftVersion.Minor) switch
        {
            // Early 1.1 - 1.2.5
            // Only 1.2.5 is supported
            (1, <= 2) => new ForgeEarlyInstance(GetLegacyName(mcVer, forgeVer), "client",
                id, appConfiguration, configuration, logger, progressReporter),
            // Early 1.3.2 - 1.5.1
            // Classic 1.5.2
            (1, <= 5) => mcVer switch
            {
                "1.5.2" => new ForgeClassicInstance(GetLegacyName(mcVer, forgeVer),
                    "minecraftforge-universal-${version}.jar", id, appConfiguration, configuration, logger, progressReporter),

                _ => new ForgeEarlyInstance(GetLegacyName(mcVer, forgeVer), "universal",
                    id, appConfiguration, configuration, logger, progressReporter)
            },
            // Classic 1.6 - 1.7.2
            // Legacy 1.7.10-pre4 & 1.7.10
            (1, <= 7) => mcVer switch
            {
                "1.7.10" => new ForgeLegacyInstance($"1.7.10-{forgeVer}-1.7.10",
                    id, appConfiguration, configuration, logger, progressReporter),

                "1.7.10-pre4" => new ForgeLegacyInstance($"1.7.10_pre4-{forgeVer}-prerelease",
                    id, appConfiguration, configuration, logger, progressReporter),

                "1.7.2" => new ForgeClassicInstance($"1.7.2-{forgeVer}-mc172", "forge-${version}-universal.jar",
                    id, appConfiguration, configuration, logger, progressReporter),

                _ => new ForgeClassicInstance(GetLegacyName(mcVer, forgeVer), "minecraftforge-universal-${version}.jar",
                    id, appConfiguration, configuration, logger, progressReporter)
            },
            // Legacy 1.8+ - 1.12.x
            (1, <= 12) => (mcVer, forgeVer) switch
            {
                ("1.8", _) => new ForgeLegacyInstance(GetLegacyName(mcVer, forgeVer),
                    id, appConfiguration, configuration, logger, progressReporter),
                ("1.8.8", _) => new ForgeLegacyInstance(GetLegacyName(mcVer, forgeVer),
                    id, appConfiguration, configuration, logger, progressReporter),
                ("1.8.9", _) => new ForgeLegacyInstance(GetLegacyNameWithMc(mcVer, forgeVer),
                    id, appConfiguration, configuration, logger, progressReporter),

                ("1.9", "12.16.1.1938") => new ForgeLegacyInstance(GetLegacyNameWithZero(mcVer, forgeVer),
                    id, appConfiguration, configuration, logger, progressReporter),

                ("1.9", _) => new ForgeLegacyInstance(GetLegacyName(mcVer, forgeVer),
                    id, appConfiguration, configuration, logger, progressReporter),

                ("1.9.4", _) => new ForgeLegacyInstance(GetLegacyNameWithMc(mcVer, forgeVer),
                    id, appConfiguration, configuration, logger, progressReporter),

                ("1.10", _) => new ForgeLegacyInstance(GetLegacyNameWithZero(mcVer, forgeVer),
                    id, appConfiguration, configuration, logger, progressReporter),

                ("1.10.2", _) or ("1.11", _) or ("1.11.2", _) or ("1.12", _) or
                    ("1.12.1", _) => new ForgeLegacyInstance(GetLegacyName(mcVer, forgeVer),
                        id, appConfiguration, configuration, logger, progressReporter),
                ("1.12.2", _) => new ForgeModernInstance(id, appConfiguration, configuration, logger, progressReporter),

                _ => new ForgeLegacyInstance(GetLegacyName(mcVer, forgeVer),
                    id, appConfiguration, configuration, logger, progressReporter)
            },
            // Modern 1.13+
            _ => new ForgeModernInstance(id, appConfiguration, configuration, logger, progressReporter)
        };
    }

    /// <summary>
    ///     Returns a list of legacy Forge support libraries for very old Minecraft versions (used to fix compatibility
    ///     issues).
    /// </summary>
    /// <param name="minecraftVersion">The Minecraft version string to match against (e.g. "1.5.2").</param>
    /// <param name="path">
    ///     Optional base path prefix for the returned library entries. Defaults to the embedded asset path
    ///     <c>"Tavstal.BeaconSDK.Assets.Fmllib."</c>. Each returned entry is the concatenation of this
    ///     prefix and the actual library filename.
    /// </param>
    /// <returns>
    ///     A list of string library identifiers (path + filename) appropriate for the given Minecraft version.
    /// </returns>
    public static List<string> GetLegacyLibraries(string minecraftVersion,
        string path = "Tavstal.BeaconSDK.Assets.Fmllib.")
    {
        var mcVersion = new Version(minecraftVersion);
        if (mcVersion.Major != 1)
            return [];

        return (mcVersion.Minor, mcVersion.Build) switch
        {
            (3, _) =>
            [
                $"{path}argo-2.25.jar",
                $"{path}asm-all-4.0.jar",
                $"{path}guava-12.0.1.jar"
            ],
            (4, _) =>
            [
                $"{path}argo-2.25.jar",
                $"{path}asm-all-4.0.jar",
                $"{path}guava-12.0.1.jar",
                $"{path}bcprov-jdk15on-147.jar"
            ],
            (5, 1) =>
            [
                $"{path}argo-small-3.2.jar",
                $"{path}asm-all-4.1.jar",
                $"{path}bcprov-jdk15on-148.jar",
                $"{path}deobfuscation_data_1.5.1.zip",
                $"{path}guava-14.0-rc3.jar",
                $"{path}scala-library.jar"
            ],
            (5, 2) =>
            [
                $"{path}argo-small-3.2.jar",
                $"{path}asm-all-4.1.jar",
                $"{path}bcprov-jdk15on-148.jar",
                $"{path}deobfuscation_data_1.5.2.zip",
                $"{path}guava-14.0-rc3.jar",
                $"{path}scala-library.jar"
            ],
            (5, _) =>
            [
                $"{path}argo-small-3.2.jar",
                $"{path}asm-all-4.1.jar",
                $"{path}bcprov-jdk15on-148.jar",
                $"{path}deobfuscation_data_1.5.zip",
                $"{path}guava-14.0-rc3.jar",
                $"{path}scala-library.jar"
            ],
            _ => []
        };
    }

    /// <summary>
    ///     Generates the standard legacy Forge installer name.
    ///     Format: "mc_version-forgemc_version-forge_version"
    ///     Example: "1.8-forge1.8-11.14.4.1563"
    /// </summary>
    private static string GetLegacyName(string mcVersion, string forgeVersion)
    {
        return $"{mcVersion}-{forgeVersion}";
    }

    /// <summary>
    ///     Generates a specific legacy Forge installer name including the full Minecraft version twice.
    ///     Format: "mc_version-forgemc_version-forge_version-mc_version"
    ///     Example: "1.8.9-forge1.8.9-11.15.1.1904-1.8.9"
    /// </summary>
    private static string GetLegacyNameWithMc(string mcVersion, string forgeVersion)
    {
        return $"{mcVersion}-{forgeVersion}-{mcVersion}";
    }

    /// <summary>
    ///     Generates a specific legacy Forge installer name with a ".0" suffix.
    ///     Format: "mc_version-forgemc_version-forge_version-mc_version.0"
    ///     Example: "1.10-forge1.10-12.18.0.2000-1.10.0"
    /// </summary>
    private static string GetLegacyNameWithZero(string mcVersion, string forgeVersion)
    {
        return $"{mcVersion}-{forgeVersion}-{mcVersion}.0";
    }
}
