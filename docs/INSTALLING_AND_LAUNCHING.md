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

Set `GameDetails.Kind` accordingly and construct the matching type. Modded instances
additionally require a loader version in `GameDetails.CustomVersion`, which the loader
manifest itself supplies (see [`BeaconSDK.Example/ExampleFabricWorker.cs`](../BeaconSDK.Example/ExampleFabricWorker.cs)).
The loader manifest is downloaded and cached by `IManifestService`.

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

These four Forge types are lower level than the factory and take extra leading
arguments that the factory computes for you (`forgeVersionName` and, for the early
and classic eras, a `universalName`/`universalFormat` pattern). Prefer
`ForgeInstance.GetForgeInstance(...)`.

---

## 3. Install

```csharp
var installed = await installer.InstallAsync(instance, progress, ct);
if (!installed) return; // check the logger for the reason
```

Installation downloads and verifies:

- the client jar and version JSON,
- the asset index and asset objects,
- the client mappings,
- libraries and native libraries (`.so`, `.dll`, `.dylib`),
- the logging configuration,
- the launch wrapper, downloaded into the libraries directory from
  `BeaconConfiguration.Libraries.LaunchWrapper`,
- the mod loader (installer, libraries and processors for modded instances),
- the legacy Forge compatibility libraries for Minecraft 1.3 - 1.5, downloaded into
  the instance `lib` directory from `BeaconConfiguration.Libraries.Forge`,
- plus any files listed in `PathDetails.CustomNativeFiles`, which are copied into the
  natives directory.

Libraries are SHA-1 verified during download. The support files configured in
`BeaconConfiguration.Libraries` carry their own SHA-1 hash, which is used to
verify the download and to reuse a copy that is already present. `InstallAsync`
returns `false` on failure and logs details through `IBeaconLogger<T>`. On success
it also populates `instance.ArgumentBuilder` and `instance.MinecraftVersionMeta`, so
read the required Java version from there:

```csharp
var major = instance.MinecraftVersionMeta.JavaVersionMeta?.MajorVersion ?? 8;
```

Installation also:

- adjusts the required Java version to 7 for Forge instances on Minecraft 1.7.10 and
  older, so read the value after `InstallAsync` instead of trusting the manifest;
- adds `-Xms`/`-Xmx` from `GameDetails.MinMemory`/`MaxMemory` when they exceed the
  values declared by the version JSON;
- switches the classpath to a `classpath.txt` file next to the game directory when the
  required Java version is 9 or newer (`ArgumentBuilder.UseClasspathFile`).

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
classpath and natives, and starts the process. A few details worth knowing:

- if `GameDetails.JavaPath` is empty, the process is started with `java` from the
  `PATH`;
- `GameDetails.PreLaunchCommand` is executed and awaited before the game starts, and
  `GameDetails.PostExitCommand` runs when the process exits;
- `GameDetails.EnvironmentVariables` entries are copied into the process environment;
- `GameDetails.WrapperCommands` is optional: the first entry is the executable and the
  rest are its arguments. An argument containing `%command%` is replaced by the resolved
  Java executable plus the JVM arguments; without it the Java executable is appended
  after the listed arguments. Leave the list empty to launch Java directly;
- for Minecraft 1.7 and older (where there is no dedicated `logs` directory), the
  launcher archives the previous `<gameDir>/logs/latest.log` and pipes the process
  output into a fresh one;
- the access token of online instances is masked (`*****`) before arguments or process
  output are written to the log;
- for Vanilla, Fabric and Quilt the launch wrapper is used, so the game
  arguments are written to the process' standard input instead of the command line.

### Managing running instances

```csharp
launcher.IsRunning(instance.Id);            // is this instance running?
await launcher.StopAsync(instance.Id, ct);  // stop it
```

`StopAsync` asks the game window to close first and only kills the process tree when
that fails. If the instance is not running it just logs a warning. Running instances
are tracked in a thread-safe collection keyed by instance id.

---

## Putting it together

See the [`BeaconSDK.Example`](https://github.com/TavstalDev/BeaconSDK/tree/master/BeaconSDK.Example)
project for complete interactive samples: `ExampleLoginWorker` asks whether to sign in
with a Microsoft account or to play offline and exposes the resulting display name, UUID
and access token to the other workers. `ExampleVanillaWorker` fetches the manifest,
installs a vanilla instance, ensures Java and launches the game, and
`ExampleFabricWorker`, `ExampleQuiltWorker`, `ExampleForgeWorker` and
`ExampleNeoForgeWorker` do the same for the respective loader instances.
