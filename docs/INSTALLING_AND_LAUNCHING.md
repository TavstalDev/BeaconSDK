# Installing and launching

Installing and launching an instance follows three steps:

1. Resolve a `MinecraftVersion` from the Mojang version manifest (via `IManifestService`).
2. Build a `MinecraftInstance` (or a modded subclass) using `BeaconConfiguration` and `InstanceConfiguration`.
3. Call `IInstanceInstallService.InstallAsync` and then
   `IInstanceLaunchService.LaunchAsync`.

---

## 1. Resolve the Minecraft version

Download the Mojang version manifest and read it through `IManifestService`:

```csharp
var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "manifest.json");
if (!File.Exists(manifestPath))
{
    await manifests.DownloadMinecraftManifestAsync(manifestPath, null, ct);
}

var manifest = await manifests.GetMinecraftManifestAsync(manifestPath, ct);
var version = manifest!.Versions.First(v => v.Id == "1.21.4");
```

`IManifestService` caches manifests and also exposes loader manifests for Fabric,
Quilt, Forge and NeoForge (`GetFabricManifestAsync`, `GetForgeManifestAsync`, …).

---

## 2. Describe the instance

Four model classes hold the configuration:

| Type | Contains |
| --- | --- |
| `GameDetails` | Java path, min/max memory (MB), JVM args, version, `EMinecraftKind`, custom version, game directory, pre/post launch commands, wrappers, environment variables, server to join |
| `PathDetails` | Assets, cache, libraries and versions directories, and optional custom native files |
| `LauncherDetails` | Launcher name and version (used in launch arguments) |
| `ClientDetails` | Access token, display name, UUID, offline flag, client ID, XUID |

```csharp
var game = new GameDetails(
    javaPath: "",                  // if it was not provided by the user then it should be set after Java is resolved
    minMemory: 2048,
    maxMemory: 8192,
    jvmArgs: "",
    minecraftVersion: version.Id,
    kind: EMinecraftKind.VANILLA,
    customVersion: null,
    customGameDirectory: null,
    preLaunchCommand: "",
    wrapperCommands: [],
    postExitCommand: "",
    environmentVariables: [],
    serverAddressToJoin: null);

var paths = new PathDetails(
    assetsDir: "game/assets",
    cacheDir: "game/cache",
    librariesDir: "game/libs",
    versionsDir: "game/versions",
    customNativeFiles: []);

var launcher = new LauncherDetails("myLauncher", "1.0.0");
var client = new ClientDetails(null, "Player", GameHelper.GetOfflinePlayerUUID("Player"), true);
```

Then construct the instance using `InstanceConfiguration`:

```csharp
var config = new InstanceConfiguration
{
    Version = version,
    Game = game,
    Path = paths,
    Launcher = launcher,
    Client = client
};

var instance = new MinecraftInstance(
    id: version.Id,
    appConfiguration: beaconConfig,
    configuration: config,
    logger: logger,
    progressReporter: progress);
```

### Modded instances

Set `GameDetails.Kind` accordingly and construct the matching type. All modded
instances share the same constructor shape and require a loader version
(`GameDetails.CustomVersion`). The loader manifest itself is downloaded and cached
by `IManifestService`.

```csharp
// Fabric
var instance = new FabricInstance(id, beaconConfig, config, logger, progress);

// Quilt
var instance = new QuiltInstance(id, beaconConfig, config, logger, progress);

// NeoForge
var instance = new NeoForgeInstance(id, beaconConfig, config, logger, progress);

// Forge - picks the right implementation automatically based on the versions
var instance = ForgeInstance.GetForgeInstance(id, beaconConfig, config, logger, progress);
```

`ForgeInstance.GetForgeInstance` returns `ForgeEarlyInstance`,
`ForgeClassicInstance`, `ForgeLegacyInstance` or `ForgeModernInstance` depending on
the Minecraft and Forge versions, so you do not have to select it yourself.

---

## 3. Install

```csharp
var installed = await installer.InstallAsync(instance, progress, ct);
if (!installed) return; // check the logger for the reason
```

Installation downloads and verifies:

- the client jar and version JSON,
- the asset index and asset objects,
- libraries and native libraries (`.so`, `.dll`, `.dylib`),
- the logging configuration,
- the mod loader (installer, libraries and processors for modded instances).

Libraries are SHA-1 verified during download. `InstallAsync` returns `false` on
failure and logs details through `IBeaconLogger<T>`.

---

## 4. Launch

Make sure Java is available (see [Java management](JAVA.md)) and set it on the
instance, then launch:

```csharp
instance.Config.Game.JavaPath = javaPath;

var process = await launcher.LaunchAsync(instance, progress, ct);
if (process is null) return;

process.Exited += (_, _) => Console.WriteLine("Game exited");
```

The launcher builds JVM and game arguments from the version metadata, applies the
classpath and natives, and starts the process.

### Managing running instances

```csharp
launcher.IsRunning(instance.Id);            // is this instance running?
await launcher.StopAsync(instance.Id, ct);  // stop it
```

`StopAsync` kills the tracked game process. Running instances are tracked in a
thread-safe collection and updated when the process exits.

---

## Putting it together

See the [`BeaconSDK.Example`](https://github.com/TavstalDev/BeaconSDK/tree/master/BeaconSDK.Example)
project for a complete interactive sample that fetches the manifest, installs a
vanilla instance, ensures Java and launches the game.
