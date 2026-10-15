# Getting started

This guide walks you through adding BeaconSDK to a .NET application, wiring up the
services and understanding the pieces you will use.

---

## Requirements

- .NET 10 SDK or runtime
- A Microsoft Azure application registration (client ID) if you want Microsoft login

---

## Install

```bash
dotnet add package BeaconSDK
```

```xml
<PackageReference Include="BeaconSDK" Version="1.0.0" />
```

The package targets `net10.0`, is trimmable/AOT compatible, and ships XML
documentation and a symbols package.

---

## Set up the host

BeaconSDK plugs into the generic host. Register the services with
`AddBeaconServices` and start the background log writer with `SetupLogger`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK;

var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.SetupLogger(); // starts the background log writer loop

var config = new BeaconConfiguration
{
    Auth = new AuthConfiguration
    {
        MicrosoftClientId = "your-azure-client-id", // optional if not using Microsoft auth
    },
    DataProtection = new DataProtectionConfig
    {
        KeysDirectory = "keys",
        ApplicationName = "myLauncher"
    }
};

hostBuilder = hostBuilder.ConfigureServices(services =>
{
    services.AddBeaconServices(config);
});

var host = hostBuilder.Build();
await host.RunAsync();
```

`AddBeaconServices` registers all SDK services as singletons, so they are safe to
inject anywhere:

- `IHttpService`, `IEncryptionService`
- `IManifestService`, `ILibraryDownloadService`
- `IInstanceInstallService`, `IInstanceLaunchService`
- `IJavaService`
- `IMicrosoftAuthService`, `IMicrosoftDeviceAuthService`, `IMicrosoftHttpAuthService`
- `IMojangSkinService`
- `IBeaconLogger<T>`

### Data protection

Token encryption uses `Microsoft.AspNetCore.DataProtection`. The keys directory configured
in `DataProtectionConfig.KeysDirectory` is where the key ring is stored. Keep this directory stable
between runs, otherwise stored tokens cannot be decrypted.

---

## Directory layout

The SDK writes everything under paths you provide. A typical layout looks like this:

```
game/
├── assets/       # downloaded assets (objects, indexes)
├── cache/        # cached manifests and metadata
├── libs/         # libraries and natives
├── versions/     # vanilla and modded version files
├── instances/    # per-instance game directories
└── java/         # auto-downloaded Java runtimes
```

You create these directories and pass them into `PathDetails`.

---

## Progress reporting

Implement `IProgressReporter` to receive progress updates. Both
`IInstanceInstallService.InstallAsync` and `IInstanceLaunchService.LaunchAsync`
accept one.

```csharp
public class ConsoleProgress : IProgressReporter
{
    public void Report(double progress) => Console.WriteLine($"{progress:0.00}%");
    public void Report(double progress, string fileName, bool downloading = true)
        => Console.WriteLine($"{(downloading ? "Downloading" : "Reading")} {fileName} {progress:0.00}%");
    public void Report(EProgressKind kind, double progress)
        => Console.WriteLine($"{kind}: {progress:0.00}%");
    public void ReportExpectedTask(int? count) { }
    public void ReportTaskCompleted() { }
}
```

`EProgressKind` describes which stage is running (reading/downloading version JSON,
assets, libraries, natives, auth steps, and so on).

---

## Logging

Inject `IBeaconLogger<T>` where needed:

```csharp
public class MyService(IBeaconLogger<MyService> logger)
{
    public void DoWork() => logger.LogInformation("Working...");
}
```

Use `BeaconBootstrap.SetupLogger()` once at startup so queued log messages are
flushed by the background writer loop.

---

## Next steps

- [Authentication](AUTHENTICATION.md) - sign in with a Microsoft account
- [Installing and launching](INSTALLING_AND_LAUNCHING.md) - install and start the game
- [Java management](JAVA.md) - locate or download the right JDK
- [API reference](API_REFERENCE.md) - full member listing
