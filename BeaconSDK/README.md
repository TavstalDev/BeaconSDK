# BeaconSDK

BeaconSDK is a .NET 10 library for building Minecraft launchers. It handles
Microsoft authentication, game file installation, mod loaders, Java runtime
management and launching, so you can focus on your launcher's UI and features.

- Full source and issue tracker: <https://github.com/TavstalDev/BeaconSDK>
- License: MIT

---

## Features

- Microsoft OAuth2 authentication (device code flow and browser redirect)
- Xbox Live / XSTS token exchange and Minecraft access tokens
- Mojang profile retrieval, plus skin and cape management
- Mojang version manifest with Fabric, Quilt, Forge and NeoForge loader manifests
- Instance installation with SHA-1 verification for jars and libraries
- Vanilla plus Fabric, Quilt, Forge (early → modern) and NeoForge instances
- Java runtime discovery, inspection and automatic download
- Argument building and process launch/stop lifecycle
- `IProgressReporter` for progress updates and `IBeaconLogger<T>` for logging
- Dependency injection integration via `Microsoft.Extensions.Hosting`

---

## Installation

```bash
dotnet add package BeaconSDK
```

```xml
<PackageReference Include="BeaconSDK" Version="1.0.0" />
```

Requires the **.NET 10** SDK or runtime.

---

## Quickstart

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK;

var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.SetupLogger();
var config = new BeaconConfiguration
{
    Auth = new AuthConfiguration
    {
        MicrosoftClientId = "your-azure-client-id",
    }
};
hostBuilder = hostBuilder.ConfigureServices(services =>
{
    services.AddBeaconServices(config);
});
var host = hostBuilder.Build();
await host.RunAsync();
```

Resolve the services you need and install a vanilla instance:

```csharp
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Domain;
using Tavstal.BeaconSDK.Instances;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Instance.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;

var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "manifest.json");
if (!File.Exists(manifestPath))
{
    await manifests.DownloadMinecraftManifestAsync(manifestPath, null, ct);
}
var manifest = await manifests.GetMinecraftManifestAsync(manifestPath, ct);
var version = manifest!.Versions.First(v => v.Id == mcVersion);

var gameDir = Path.Combine(Directory.GetCurrentDirectory(), "game", "instances", mcVersion);
Directory.CreateDirectory(gameDir);
var game = new GameDetails("", 1024, 4096, "", mcVersion, EMinecraftKind.VANILLA,
    null, gameDir, "", [], "", [], null);
var paths = new PathDetails("game/assets", "game/cache", "game/libs", "game/versions", []);
var launcherDetails = new LauncherDetails("myLauncher", "1.0.0");
var client = new ClientDetails(null, username, GameHelper.GetOfflinePlayerUUID(username), true);

var instanceConfig = new InstanceConfiguration
{
    Version = version,
    Game = game,
    Path = paths,
    Launcher = launcherDetails,
    Client = client
};
var instance = new MinecraftInstance(mcVersion, config, instanceConfig,
    new BeaconLogger<MinecraftInstance>(Microsoft.Extensions.Logging.LogLevel.Information), progress);

await installer.InstallAsync(instance, progress, ct);
var process = await launcher.LaunchAsync(instance, progress, ct);
```

For a complete, runnable example, see the
[`BeaconSDK.Example`](https://github.com/TavstalDev/BeaconSDK/tree/master/BeaconSDK.Example) project.

---

## Documentation

- [Getting started](https://github.com/TavstalDev/BeaconSDK/blob/master/docs/GETTING_STARTED.md)
- [Authentication](https://github.com/TavstalDev/BeaconSDK/blob/master/docs/AUTHENTICATION.md)
- [Installing and launching](https://github.com/TavstalDev/BeaconSDK/blob/master/docs/INSTALLING_AND_LAUNCHING.md)
- [Java management](https://github.com/TavstalDev/BeaconSDK/blob/master/docs/JAVA.md)
- [API reference](https://github.com/TavstalDev/BeaconSDK/blob/master/docs/API_REFERENCE.md)

---

## Contributing

See [docs/CONTRIBUTING.md](https://github.com/TavstalDev/BeaconSDK/blob/master/docs/CONTRIBUTING.md) for guidelines on how to help with this project.

---

## License

This project is licensed under the **MIT License**. See the
[LICENSE](https://github.com/TavstalDev/BeaconSDK/blob/master/LICENSE) file for more details.

## Contact

For issues or feature requests, use the
[GitHub issue tracker](https://github.com/TavstalDev/BeaconSDK/issues).
