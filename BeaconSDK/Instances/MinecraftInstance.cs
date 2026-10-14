using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Instance.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Instances;

/// <summary>
///     Represents a configurable Minecraft instance used by the launcher. This class encapsulates
///     paths, version metadata and runtime configuration for an instance and provides common
///     installation and argument-building helpers used by derived instance types (vanilla, Fabric/Quilt, Forge, etc.).
/// </summary>
public class MinecraftInstance
{
    /// <summary>
    ///     Gets the logger used to write diagnostic and progress messages.
    /// </summary>
    protected readonly IBeaconLogger _logger;

    /// <summary>
    ///     Gets the SDK-wide configuration providing endpoint, HTTP, authentication and data protection settings.
    /// </summary>
    protected readonly BeaconConfiguration _appConfiguration;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MinecraftInstance" /> class and prepares
    ///     the <see cref="VersionData" /> by computing file-system locations for vanilla and custom versions.
    /// </summary>
    /// <param name="id">Unique instance identifier.</param>
    /// <param name="appConfiguration">The Beacon SDK configuration providing endpoint, HTTP, authentication and data protection settings.</param>
    /// <param name="configuration">The configuration describing the game, paths, launcher and client settings for the instance.</param>
    /// <param name="logger">Logger used to write diagnostic and progress messages.</param>
    /// <param name="progressReporter">Optional progress reporter for long-running tasks.</param>
    public MinecraftInstance(string id, BeaconConfiguration appConfiguration, InstanceConfiguration configuration,
        IBeaconLogger logger, IProgressReporter? progressReporter = null)
    {
        Id = id;
        _appConfiguration = appConfiguration;
        Config = configuration;
        _logger = logger;
        _progressReporter = progressReporter;

        var vanillaVersionsRoot = Path.Combine(Config.Path.VersionsDir, "vanilla");
        Directory.CreateDirectory(vanillaVersionsRoot);
        var vanillaVersionDir = Path.Combine(vanillaVersionsRoot, Config.Game.MinecraftVersion);

        VersionData = new VersionDetails
        {
            MinecraftVersion = Config.Game.MinecraftVersion,
            CustomVersion = Config.Game.CustomVersion,
            VanillaVersionDirectory = vanillaVersionDir,
            VanillaJarPath = Path.Combine(vanillaVersionDir, $"{Config.Game.MinecraftVersion}.jar"),
            VanillaJsonPath = Path.Combine(vanillaVersionDir, $"{Config.Game.MinecraftVersion}.json")
        };

        var hasCustomGameDir = string.IsNullOrEmpty(Config.Game.CustomGameDirectory);
        if (Config.Game.Kind != EMinecraftKind.VANILLA)
        {
            var customVersionRoot = Path.Combine(Config.Path.VersionsDir, Config.Game.Kind.ToString().ToLower());
            Directory.CreateDirectory(customVersionRoot);
            var customVersionName = $"{Config.Game.MinecraftVersion}-{Config.Game.CustomVersion}";
            var customVersionDir = Path.Combine(customVersionRoot, customVersionName);
            var isFabric = Config.Game.Kind is EMinecraftKind.FABRIC or EMinecraftKind.QUILT;

            VersionData.CustomVersionDirectory = customVersionDir;
            VersionData.CustomJarPath = isFabric
                ? VersionData.VanillaJarPath
                : Path.Combine(customVersionDir, $"{customVersionName}.jar");
            VersionData.CustomJsonPath = Path.Combine(customVersionDir, $"{customVersionName}.json");
            VersionData.GameDir =
                hasCustomGameDir ? Path.Combine(customVersionDir, "game") : Config.Game.CustomGameDirectory!;
            VersionData.NativesDir = hasCustomGameDir
                ? Path.Combine(customVersionDir, "natives")
                : Path.Combine(VersionData.GameDir, "natives");
        }
        else
        {
            VersionData.GameDir = hasCustomGameDir
                ? Path.Combine(vanillaVersionDir, "game")
                : Config.Game.CustomGameDirectory!;
            VersionData.NativesDir = hasCustomGameDir
                ? Path.Combine(vanillaVersionDir, "natives")
                : Path.Combine(VersionData.GameDir, "natives");
        }
    }

    /// <summary>
    ///     Gets the unique identifier of this instance.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     Gets or sets the configuration of this instance, describing the game, paths, launcher and client settings.
    /// </summary>
    public InstanceConfiguration Config { get; set; }

    /// <summary>
    ///     Gets the version metadata resolved for this instance, containing the vanilla and custom
    ///     version directories and jar/json paths.
    /// </summary>
    public VersionDetails VersionData { get; }

    /// <summary>
    ///     Gets or sets the argument builder used to produce the JVM and game argument strings.
    ///     It is usually created during installation and is <see langword="null" /> before that.
    /// </summary>
    public ArgumentBuilder? ArgumentBuilder { get; set; }

    /// <summary>
    ///     Gets the optional progress reporter used to report progress of long-running tasks.
    /// </summary>
    protected IProgressReporter? _progressReporter { get; }

    /// <summary>
    ///     Gets or sets the version metadata of the version that will actually be launched.
    ///     This is the modded version metadata for modded instances and the vanilla one otherwise.
    /// </summary>
    public VersionMeta MinecraftVersionMeta { get; set; } = null!;

    /// <summary>
    ///     Builds the combined list of libraries required to launch the instance by merging
    ///     the vanilla <see cref="VersionMeta.Libraries" /> with any modded libraries provided by
    ///     <paramref name="moddedData" />.
    /// </summary>
    /// <param name="moddedData">Optional modded data returned by <see cref="InstallModdedAsync" /> (may be null for vanilla).</param>
    /// <returns>
    ///     A list where modded libraries (if present) are placed before the vanilla libraries, preserving order.
    /// </returns>
    public List<LibraryMeta> GetCombinedLibraries(ModdedData? moddedData)
    {
        var libraries = new List<LibraryMeta>(MinecraftVersionMeta.Libraries);
        if (moddedData?.Libraries.Count > 0)
            libraries.InsertRange(0, moddedData.Libraries);
        return libraries;
    }

    /// <summary>
    ///     Performs modloader-specific installation steps (if applicable) and returns any modded launch metadata.
    /// </summary>
    /// <param name="tempDir">Temporary directory that may be used for downloads and extraction.</param>
    /// <param name="httpService">HTTP service used for remote downloads.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    ///     A <see cref="ModdedData" /> instance containing a custom main class and additional libraries when a modloader is
    ///     used,
    ///     or <c>null</c> for vanilla instances. Derived classes override this method to implement installation logic.
    /// </returns>
    public virtual Task<ModdedData?> InstallModdedAsync(string tempDir, IHttpService httpService,
        CancellationToken cancellationToken = default)
    {
        // Vanilla installer, do nothing
        return Task.FromResult<ModdedData?>(null);
    }

    /// <summary>
    ///     Computes a version name string used in version directories and classpath selection.
    /// </summary>
    /// <param name="modVersion">Optional modloader version (e.g. Fabric loader, Forge, NeoForge).</param>
    /// <returns>A formatted version name appropriate for the configured <see cref="GameDetails.Kind" />.</returns>
    public string GetVersionName(string? modVersion)
    {
        return Config.Game.Kind switch
        {
            EMinecraftKind.VANILLA => VersionData.MinecraftVersion,
            EMinecraftKind.FABRIC => $"fabric-loader-{modVersion}-{VersionData.MinecraftVersion}",
            EMinecraftKind.QUILT => $"quilt-loader-{modVersion}-{VersionData.MinecraftVersion}",
            EMinecraftKind.FORGE => $"{VersionData.MinecraftVersion}-forge-{modVersion}",
            EMinecraftKind.NEOFORGE => $"{VersionData.MinecraftVersion}-neoforge-{modVersion}",
            _ => VersionData.MinecraftVersion
        };
    }
}
