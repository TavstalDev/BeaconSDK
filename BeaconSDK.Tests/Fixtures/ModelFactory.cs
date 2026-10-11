using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Installer;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;

namespace Tavstal.BeaconSDK.Tests.Fixtures;

/// <summary>
///     Central builders for the SDK models used across the test suite.
/// </summary>
public static class ModelFactory
{
    public const string DefaultGameArgumentTemplate =
        "--username ${auth_player_name} --accessToken ${auth_access_token} --uuid ${auth_uuid} " +
        "--userType ${user_type} --version ${version_name}";

    public static VersionMeta CreateVersionMeta(string id = "1.20.4",
        string gameArguments = DefaultGameArgumentTemplate)
    {
        return new VersionMeta
        {
            Id = id,
            Type = "release",
            ArgumentsLegacy = gameArguments,
            Assets = "5"
        };
    }

    public static LauncherDetails CreateLauncherDetails(string name = "TestLauncher", string version = "1.0.0")
    {
        return new LauncherDetails(name, version);
    }

    public static GameDetails CreateGameDetails(
        string javaPath = "/usr/bin/java",
        uint minMemory = 512,
        uint maxMemory = 2048,
        string jvmArgs = "",
        string minecraftVersion = "1.20.4",
        EMinecraftKind kind = EMinecraftKind.VANILLA,
        string? customVersion = null,
        string? customGameDirectory = null,
        string preLaunchCommand = "",
        List<string>? wrapperCommands = null,
        string postExitCommand = "",
        Dictionary<string, string>? environmentVariables = null,
        string? serverAddressToJoin = null)
    {
        return new GameDetails(javaPath, minMemory, maxMemory, jvmArgs, minecraftVersion, kind, customVersion,
            customGameDirectory, preLaunchCommand, wrapperCommands ?? [], postExitCommand,
            environmentVariables ?? new Dictionary<string, string>(), serverAddressToJoin);
    }

    public static ClientDetails CreateClientDetails(
        string? accessToken = "access-token",
        string displayName = "Player",
        string uuid = "00000000-0000-0000-0000-000000000000",
        bool isOffline = false,
        string clientId = "0",
        string xuid = "0")
    {
        return new ClientDetails(accessToken, displayName, uuid, isOffline, clientId, xuid);
    }

    public static PathDetails CreatePathDetails(
        string assetsDir = "/assets",
        string cacheDir = "/cache",
        string librariesDir = "/libraries",
        string versionsDir = "/versions",
        string manifestPath = "/manifest.json",
        string? customManifestPath = null,
        List<string>? customNativeFiles = null)
    {
        return new PathDetails(assetsDir, cacheDir, librariesDir, versionsDir, manifestPath, customManifestPath,
            customNativeFiles ?? []);
    }

    public static Resolution CreateResolution(uint x = 854, uint y = 480)
    {
        return new Resolution(x, y);
    }
}
