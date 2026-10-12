using Tavstal.BeaconSDK.Models.Instance.Config;
using Tavstal.BeaconSDK.Models.MojangApi;

namespace Tavstal.BeaconSDK.Models.Instance;

/// <summary>
///     Represents the configuration of a game instance, including game, path, launcher, client details,
///     and an optional resolution.
/// </summary>
public class InstanceConfiguration
{
    /// <summary>
    ///     Gets or sets the Minecraft version of the game instance.
    /// </summary>
    public required MinecraftVersion Version { get; set; }

    /// <summary>
    ///     Gets or sets the details of the game to be launched.
    /// </summary>
    public required GameDetails Game { get; set; }

    /// <summary>
    ///     Gets or sets the details of the instance paths.
    /// </summary>
    public required PathDetails Path { get; set; }

    /// <summary>
    ///     Gets or sets the details of the launcher.
    /// </summary>
    public required LauncherDetails Launcher { get; set; }

    /// <summary>
    ///     Gets or sets the details of the client.
    /// </summary>
    public required ClientDetails Client { get; set; }

    /// <summary>
    ///     Gets or sets the resolution for the game window, if specified.
    /// </summary>
    public Resolution? Resolution { get; set; }
}
