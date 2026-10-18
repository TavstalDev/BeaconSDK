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

BeaconSDK plugs into the generic host. Register the services with `AddBeaconServices`;
the log writer is registered as a hosted service, so starting the host is enough to
begin flushing log entries to disk:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK;

var hostBuilder = Host.CreateDefaultBuilder(args);

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
    },
    Logging = new LoggingConfig
    {
        LogLevel = LogLevel.Information,
        DefaultLogFilePath = Path.Combine(logsDir, "latest.log")
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
- `IBeaconLogger<T>`, `ILogWriter`

### Data protection

Token encryption uses `Microsoft.AspNetCore.DataProtection`. The keys directory configured
in `DataProtectionConfig.KeysDirectory` is where the key ring is stored. Keep this directory stable
between runs, otherwise stored tokens cannot be decrypted.

`DataProtectionConfig` also exposes `ProtectorPurpose` (the data protection purpose string),
`ApplicationName`, and the per-platform fallback material `WindowsEntropy`, `LinuxKey` and
`MacKey`. The fallbacks are only used when no `IDataProtectionProvider` is registered, so
prefer the DI-registered provider that `AddBeaconServices` sets up for you.

### HTTP and endpoints

`BeaconConfiguration.Http` (`HttpConfig`) tunes the `HttpClient` the SDK uses:
`Agent`, `AcceptHeader`, `Timeout`, `AutomaticDecompression`, `AllowAutoRedirect`,
`EnableMultipleHttp2Connections`, `MaxParallelDownloads`, `BufferSize`, `TempFileSuffix`
and `EnableSslProtocols`.

`BeaconConfiguration.Endpoints` (`EndpointConfig`) holds the base URLs for Microsoft, Mojang,
Fabric, Quilt, Forge and NeoForge, in case you need to point the SDK at mirrors or proxies.

---

## Directory layout

The SDK writes everything under paths you provide. A typical layout looks like this:

```
game/
├── assets/       # downloaded assets (objects, indexes)
├── cache/        # cached manifests and metadata
├── libs/         # libraries, natives and the extracted launch wrapper
├── versions/     # vanilla and modded version files
│   ├── vanilla/<mcVersion>/
│   └── fabric/<mcVersion>-<loaderVersion>/
├── instances/    # per-instance game directories
└── java/         # auto-downloaded Java runtimes
```

You point the SDK at these paths through `PathDetails` and
`GameDetails.CustomGameDirectory`. You are responsible for the directories you hand over,
but the SDK creates the ones it needs inside them: the `versions/vanilla` and per-loader
version directories are created by the `MinecraftInstance` constructor, and the game and
natives directories are created during installation.

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

`AddBeaconServices` registers `IBeaconLogger<>` as a singleton whose `BeaconLogger<T>`
is built from your `BeaconConfiguration.Logging`. Entries below
`LoggingConfig.LogLevel` are discarded; the rest are printed to the console and, when
`LoggingConfig.DefaultLogFilePath` is set, appended to that file. Parent directories
are created as needed, so unlike previous versions you no longer have to create the
target folder yourself:

```csharp
var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");

var config = new BeaconConfiguration
{
    Auth = new AuthConfiguration { MicrosoftClientId = "your-azure-client-id" },
    Logging = new LoggingConfig
    {
        LogLevel = LogLevel.Information,
        DefaultLogFilePath = Path.Combine(logsDir, "latest.log")
    }
};
```

Leaving `DefaultLogFilePath` null (the default) keeps everything on the console and
discards file output.

Loggers you construct yourself take the same options explicitly:

```csharp
// custom file for this logger only
var logger = new BeaconLogger<MyService>(LogLevel.Information, customLogFilePath: logPath);

// console only, nothing written to disk
var consoleLogger = new BeaconLogger<MyService>(LogLevel.Debug, printLogs: true, customLogFilePath: null);
```

### Forwarding to Microsoft.Extensions.Logging

Set `LoggingConfig.EnableCustomLogger` to `false` to have `BeaconLogger<T>` hand its
output to the `ILogger<T>` your host already registered, instead of writing to the
console. This lets a host-wide logging setup (Serilog, console providers, OpenTelemetry)
capture SDK output:

```csharp
var config = new BeaconConfiguration
{
    Auth = new AuthConfiguration { MicrosoftClientId = "your-azure-client-id" },
    Logging = new LoggingConfig
    {
        EnableCustomLogger = false,
        LogLevel = LogLevel.Debug
    }
};
```

`AddBeaconServices` registers a `NullLogger<>` fallback for `ILogger<>`, so this works
even when the host never called `AddLogging()`. File logging via
`LoggingConfig.DefaultLogFilePath` is unaffected either way.

### Writing to the log file yourself

Inject `ILogWriter` and call `EnqueueLog`. It returns immediately; the hosted
`LogWriter` drains the queue on a background loop. Passing no path uses
`LoggingConfig.DefaultLogFilePath`, and an entry with no resolvable path is dropped:

```csharp
public class MyService(ILogWriter logWriter)
{
    public void DoWork() => logWriter.EnqueueLog("raw line");
}
```

Entries are only written while the host is running, because the writer is a hosted
service. Rotate the file yourself at startup, before `AddBeaconServices`.

### Log rotation

Rotation is not built in, so archives are your app's job. The
[`BeaconSDK.Example`](https://github.com/TavstalDev/BeaconSDK/blob/master/BeaconSDK.Example/Program.cs)
does it at startup, before `AddBeaconServices`: move the previous `latest.log` to a
timestamped name and compress it with `FileSystemHelper.CompressFile`.

```csharp
string latestLog = Path.Combine(logsDir, "latest.log");
var fileInfo = new FileInfo(latestLog);
if (fileInfo.Exists)
{
    var newPath = Path.Combine(logsDir, $"{fileInfo.LastWriteTime:yyyy-MM-dd_HH-mm-ss}.log");
    var archivePath = newPath + ".gz";
    File.Move(latestLog, newPath, true);
    FileSystemHelper.CompressFile(newPath, archivePath);
}
```

---

## Next steps

- [Authentication](AUTHENTICATION.md) - sign in with a Microsoft account
- [Installing and launching](INSTALLING_AND_LAUNCHING.md) - install and start the game
- [Java management](JAVA.md) - locate or download the right JDK
- [API reference](API_REFERENCE.md) - full member listing
