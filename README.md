# BeaconSDK

![Release (latest by date)](https://img.shields.io/github/v/release/TavstalDev/BeaconSDK?style=plastic-square)
![Workflow Status](https://img.shields.io/github/actions/workflow/status/TavstalDev/BeaconSDK/release.yml?branch=stable&label=build&style=plastic-square)
![License](https://img.shields.io/github/license/TavstalDev/BeaconSDK?style=plastic-square)
![Downloads](https://img.shields.io/github/downloads/TavstalDev/BeaconSDK/total?style=plastic-square)
![Stars](https://img.shields.io/github/stars/TavstalDev/BeaconSDK?style=plastic-square)
![Issues](https://img.shields.io/github/issues/TavstalDev/BeaconSDK?style=plastic-square)
![Forks](https://img.shields.io/github/forks/TavstalDev/BeaconSDK?style=plastic-square)

BeaconSDK is a .NET library for building Minecraft launchers. It handles the parts
that are tedious to get right: Microsoft/Xbox authentication, downloading and
verifying game files, installing mod loaders, managing Java runtimes and launching
the game with a correct argument list.

It targets **.NET 10**, is trim/AOT friendly, and integrates with
`Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Hosting`.

---

## Features

- **Microsoft authentication** — OAuth2 device code flow and browser redirect flow,
  Xbox Live / XSTS token exchange, Minecraft access tokens, ownership checks, and
  profile retrieval.
- **Account management** — `Account` model with encrypted access/refresh tokens at
  rest and login refresh support.
- **Mojang skins & capes** — change, upload, reset skins and show/hide capes.
- **Minecraft version manifests** — cached Mojang version manifest plus Fabric,
  Quilt, Forge and NeoForge loader manifests.
- **Instance installation** — downloads and verifies the client jar, libraries,
  assets, natives and logging config, with SHA-1 verification.
- **Mod loader support** — Fabric, Quilt, Forge (early → modern) and NeoForge,
  including the legacy Forge compatibility libraries.
- **Java management** — locates installed JVMs, inspects their version/architecture,
  and downloads the required JDK automatically.
- **Launching** — builds JVM and game arguments and starts the game process, with
  start/stop lifecycle and running-process tracking.
- **Progress reporting** — a single `IProgressReporter` surfaces every stage to your UI.
- **Logging** — `IBeaconLogger<T>` with a background writer loop, integrated with
  the generic host.
- **Resilient downloads** — gzip/TLS handling, parallel downloads and integrity checks.

---

## Requirements

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- A Microsoft Azure application registration (client ID) if you use Microsoft login

---

## Installation

```bash
dotnet add package BeaconSDK
```

Or add the package reference directly:

```xml
<PackageReference Include="BeaconSDK" Version="1.0.0" />
```

---

## Quickstart

Register the SDK services on a host and start the logger:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK;

var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.SetupLogger();
hostBuilder = hostBuilder.ConfigureServices(services =>
{
    services.AddBeaconServices("keysDir", "myLauncher");
});
var host = hostBuilder.Build();
await host.RunAsync();
```

`AddBeaconServices` registers every service (HTTP, encryption, manifests, install,
launch, Java and authentication). Inject what you need, then install and launch an
instance:

```csharp
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Domain;
using Tavstal.BeaconSDK.Instances;
using Tavstal.BeaconSDK.Models.Endpoints;
using Tavstal.BeaconSDK.Models.Installer;
using Tavstal.BeaconSDK.Services.Abstractions;

public class Launcher(IHttpService http, IManifestService manifests,
    IInstanceInstallService installer, IInstanceLaunchService launcher,
    IJavaService java, IBeaconLogger<Launcher> log, IProgressReporter progress)
{
    public async Task RunAsync(string username, string mcVersion, CancellationToken ct)
    {
        // 1. Fetch the Minecraft version manifest
        var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "manifest.json");
        await http.DownloadFileAsync(MicrosoftEndpoints.MinecraftManifestUrl, manifestPath, null, null, ct);
        var manifest = await manifests.GetMinecraftManifestAsync(manifestPath, ct);
        var version = manifest!.Versions.First(v => v.Id == mcVersion);

        // 2. Describe the instance
        var game = new GameDetails(javaPath: "", 1024, 4096, "", mcVersion, EMinecraftKind.VANILLA,
            customVersion: null, customGameDirectory: null, preLaunchCommand: "",
            wrapperCommands: [], postExitCommand: "", environmentVariables: [], serverAddressToJoin: null);
        var paths = new PathDetails(assetsDir: "game/assets", cacheDir: "game/cache",
            librariesDir: "game/libs", versionsDir: "game/versions", manifestPath, null, []);
        var launcherDetails = new LauncherDetails("myLauncher", "1.0.0");
        var uuid = GameHelper.GetOfflinePlayerUUID(username);
        var client = new ClientDetails(null, username, uuid, isOffline: true);

        var instance = new MinecraftInstance(mcVersion, version, game, paths, launcherDetails, client, log, null, progress);

        // 3. Install, ensure Java, then launch
        if (!await installer.InstallAsync(instance, progress, ct)) return;

        var major = instance.MinecraftVersionMeta.JavaVersionMeta?.MajorVersion ?? 8;
        await java.LocateJavaInstallationsAsync(Path.Combine("game", "java"), cancellationToken: ct);
        var javaPath = await java.GetJavaVersionAsync(major, ct)
            ?? await java.DownloadJavaVersionAsync(major, "game/java", "java-mirrors.json", null, ct);
        instance.GameDetails.JavaPath = javaPath;

        var process = await launcher.LaunchAsync(instance, progress, ct);
    }
}
```

A complete, runnable sample lives in
[`BeaconSDK.Example`](BeaconSDK.Example). For modded instances, construct a
`FabricInstance`, `QuiltInstance`, `ForgeInstance.GetForgeInstance(...)` or
`NeoForgeInstance` and pass it to the same services.

---

## Core concepts

| Type | Purpose |
| --- | --- |
| `BeaconBootstrap.AddBeaconServices` | Registers all SDK services in DI |
| `BeaconBootstrap.SetupLogger` | Starts the background log writer |
| `MinecraftInstance` | Describes an instance (version, paths, memory, client) |
| `FabricInstance`, `QuiltInstance`, `NeoForgeInstance`, `ForgeInstance` | Mod loader installers |
| `GameDetails` / `PathDetails` / `LauncherDetails` / `ClientDetails` | Instance configuration |
| `IProgressReporter` | Receives progress/status updates |
| `IBeaconLogger<T>` | Structured logger used throughout the SDK |

| Service | Purpose |
| --- | --- |
| `IHttpService` | GET/POST, streaming, downloads, parallel downloads |
| `IManifestService` | Mojang + mod loader manifests (cached) |
| `IInstanceInstallService` | Downloads and verifies all instance files |
| `IInstanceLaunchService` | Builds arguments and starts/stops the game |
| `IJavaService` | Discovers, inspects and downloads Java runtimes |
| `IMicrosoftAuthService` | Microsoft/Xbox/Minecraft authentication |
| `IMicrosoftDeviceAuthService` | Device code polling loop |
| `IMicrosoftHttpAuthService` | OAuth2 redirect callback listener |
| `IMojangSkinService` | Skin and cape management |
| `IEncryptionService` | Token encryption/decryption |

---

## Documentation

- [Getting started](docs/GETTING_STARTED.md)
- [Authentication](docs/AUTHENTICATION.md)
- [Installing and launching](docs/INSTALLING_AND_LAUNCHING.md)
- [Java management](docs/JAVA.md)
- [API reference](docs/API_REFERENCE.md)

---

## Contributing

See [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) for guidelines on how to help with this project.

---

## License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for more details.

## Contact

For issues or feature requests, use the
[GitHub issue tracker](https://github.com/TavstalDev/BeaconSDK/issues).
