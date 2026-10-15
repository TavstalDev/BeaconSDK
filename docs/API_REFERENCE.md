# API reference

A high-level map of the public API. Each type also ships with XML documentation in
the NuGet package.

- [Root namespace](#root-namespace)
- [Services](#services)
- [Instances](#instances)
- [Models](#models)
- [Enums](#enums)
- [Helpers](#helpers)
- [Endpoints](#endpoints)

---

## Root namespace

`Tavstal.BeaconSDK`

### `BeaconBootstrap` (static)

| Member | Description |
| --- | --- |
| `Task LoggerTask { get; }` | Background log-writer task created by `SetupLogger` |
| `IHostBuilder SetupLogger(this IHostBuilder, CancellationToken = default)` | Starts the background log-writer loop (idempotent) |
| `IServiceCollection AddBeaconServices(this IServiceCollection, BeaconConfiguration configuration)` | Registers all SDK services |

---

## Services

`Tavstal.BeaconSDK.Services.Abstractions`

### `IHttpService`

`CreateHttpClient`, `GetAsync`, `GetStringAsync` (with/without progress),
`GetByteArrayAsync`, `GetStreamAsync`, `DownloadFileAsync`, `PostAsync`.

### `IEncryptionService`

`Encrypt(byte[])`, `Decrypt(byte[], bool ignoreUnknown = false)`,
`Reprotect(byte[])`. Throws `CryptographicException` on unknown formats or when
data protection is unavailable.

### `IManifestService`

Cached Mojang and loader manifests: `GetMinecraftManifest[Async]`, `DownloadMinecraftManifestAsync`,
`GetFabricManifest[Async]`, `DownloadFabricManifestAsync`, `GetQuiltManifest[Async]`, `DownloadQuiltManifestAsync`,
`GetForgeManifest[Async]`, `DownloadForgeManifestAsync`, `GetNeoForgeManifest[Async]`, `DownloadNeoForgeManifestAsync`.

### `IInstanceInstallService`

`Task<bool> InstallAsync(MinecraftInstance, IProgressReporter?, CancellationToken)`.

### `IInstanceLaunchService`

`LaunchAsync(...) → Process?`, `StopAsync(string instanceId, ...)`,
`IsRunning(string instanceId) → bool`.

### `IJavaService`

`DownloadJavaVersionAsync`, `GetJavaVersionAsync`, `IsJavaInstalledAsync`,
`GetJavaVersionDetailsAsync`, `LocateJavaInstallationsAsync`.

### `ILibraryDownloadService`

Resolves and downloads libraries and natives for an instance. Used internally by
`IInstanceInstallService`.

### `IMojangSkinService`

`ChangeSkinAsync`, `UploadSkinAsync`, `ResetSkinAsync`, `ShowCapeAsync`,
`HideCapeAsync`.

`Tavstal.BeaconSDK.Services.Abstractions.Auth`

### `IMicrosoftAuthService`

Properties: `AuthStatus`, `MojangProfile`, `Account`; event `OnAuthStatusChanged`.

Methods: `Reset`, `OpenAuthenticationUrl`,
`HandleHttpRequestAsync`, `CreateDeviceCodeAsync`, `CheckDeviceCodeAsync`,
`XboxTokenCallAsync`, `XboxXstsCallAsync`, `MinecraftAccessCallAsync`,
`CheckMinecraftOwnershipAsync`, `GetMinecraftProfileAsync`,
`FetchMinecraftProfileAsync`, `RefreshLoginAsync`.

Nested delegate: `AuthStatusChangedHandler(EAuthStatus status)`.

### `IMicrosoftDeviceAuthService`

`StartListeningAsync(string deviceCode, TimeSpan interval, CancellationToken)`,
`StopListeningAsync`.

### `IMicrosoftHttpAuthService`

`StartListeningAsync(IProgressReporter?, CancellationToken)`,
`StopListeningAsync(bool cancelled = true, CancellationToken)`.

The concrete implementation `MicrosoftHttpAuthService` exposes the constant
`ListeningUrl = "http://localhost:43319/"`.

`Tavstal.BeaconSDK.Services.Implementations` contains the default implementations:
`HttpService`, `EncryptionService`, `ManifestService`, `LibraryDownloadService`,
`InstanceInstallService`, `InstanceLaunchService`, `JavaService`,
`MojangSkinService`, `MicrosoftAuthService`, `MicrosoftDeviceAuthService`,
`MicrosoftHttpAuthService`.

---

## Instances

`Tavstal.BeaconSDK.Instances`

| Type | Description |
| --- | --- |
| `MinecraftInstance` | Base instance. Constructor takes `(id, BeaconConfiguration, InstanceConfiguration, IBeaconLogger, IProgressReporter?)`. Key members: `Config`, `VersionData`, `MinecraftVersionMeta`, `ArgumentBuilder`, `InstallModdedAsync`, `GetVersionName`. |
| `FabricInstance`, `QuiltInstance`, `NeoForgeInstance` | Modded instances with constructor `(id, BeaconConfiguration, InstanceConfiguration, IBeaconLogger, IProgressReporter?)`. |
| `ForgeInstance` (static) | `GetForgeInstance(...)` returns the right Forge implementation. `GetLegacyLibraries(string mcVersion, string path = ...)`. |
| `ForgeEarlyInstance`, `ForgeClassicInstance`, `ForgeLegacyInstance`, `ForgeModernInstance` | Forge implementations for each era. |
| `Forge/ForgeInstanceBase` | Shared Forge/NeoForge install logic. |

---

## Models

`Tavstal.BeaconSDK.Models`

| Type | Notes |
| --- | --- |
| `IProgressReporter` | `Report(double)`, `Report(double, string, bool)`, `Report(EProgressKind, double)`, `ReportExpectedTask(int?)`, `ReportTaskCompleted()` |
| `DownloadEntry` | `Url`, `Path`, `Sha1` |
| `EProgressKind` | Progress-stage enum (see Enums) |

`Tavstal.BeaconSDK.Models.Instance.Config`

| Type | Notes |
| --- | --- |
| `GameDetails` | Java path, memory, JVM args, version, kind, custom version/dir, pre/post commands, wrappers, env vars, server |
| `PathDetails` | Assets/cache/libraries/versions dirs, custom natives |
| `LauncherDetails` | `LauncherName`, `LauncherVersion` |
| `ClientDetails` | `AccessToken`, `DisplayName`, `UUID`, `ClientId`, `Xuid`, `IsOffline` |
| `LaunchArg` | A launch argument with an associated rule index |

`Tavstal.BeaconSDK.Models.Instance`

| Type | Notes |
| --- | --- |
| `InstanceConfiguration` | Aggregates version, game, path, launcher and client config |
| `ModdedData` | `MainClass`, `Libraries` |
| `ArgumentBuilder` | Builds JVM and game argument strings |
| `VersionDetails` | Resolved vanilla/custom version paths |
| `Resolution` | Window `Width`/`Height` |

`Tavstal.BeaconSDK.Models.Accounts`

| Type | Notes |
| --- | --- |
| `Account` | `Id`, `Uuid`, `DisplayName`, `Type`, `AccessTokenExpireDate`, `Skins`, `MojangProfile`, `CanExpire`; `Get/SetAccessToken`, `Get/SetRefreshToken` |
| `AccountSkin` | Skin/cape metadata |

`Tavstal.BeaconSDK.Models.Java`

`JavaVersion` (`Major`, `Version`, `Architecture`, `Path`), `JavaMirrorConfig`,
`JavaMirrorJdks`, `JavaMirrorArchitecture`.

`Tavstal.BeaconSDK.Models.Logging`

`IBeaconLogger` / `IBeaconLogger<T>`, `BeaconLogger` / `BeaconLogger<T>`,
`BeaconLoggerExtensions` (`LogDebug`, `LogTrace`, `LogInformation`, `LogWarning`,
`LogError`, `LogCritical`, `Log`), `LogEntry` (record).

`Tavstal.BeaconSDK.Models.Microsoft`

Request/response models for the auth chain: `DeviceCodeResult`, `XboxTokenRequestBody`,
`XboxTokenProperties`, `XboxXstsRequestBody`, `XboxXstsProperties`,
`MinecraftAccessRequestBody`.

`Tavstal.BeaconSDK.Models.MojangApi`

`VersionManifest`, `VersionManifestLatest`, `MinecraftVersion`; metadata under
`.Meta` (`VersionMeta`, `ArgumentMeta`, `AssetMeta`, `LibraryMeta`, `Rule`, …);
user models under `.User` (`MojangProfile`, `Skin`, `Cape`, `OwnershipData`, …).

`.Models.ModLoaders` holds Fabric/Quilt/Forge/NeoForge manifest models.
`.Models.Json` holds the `System.Text.Json` source-generation contexts.

---

## Enums

`Tavstal.BeaconSDK.Enums`

| Enum | Values |
| --- | --- |
| `EMinecraftKind` | `VANILLA`, `NEOFORGE`, `FORGE`, `FABRIC`, `QUILT` |
| `EAuthStatus` | `NONE`, `PENDING`, `PROCESSING`, `SUCCESS`, `FAILED` |
| `EAccountType` | `OFFLINE`, `MICROSOFT` |
| `EOperatingSystem` | `WINDOWS`, `LINUX`, `MACOS`, `UNKNOWN` |

`EProgressKind` (in `Models`) lists every progress stage from
`INSTANCE_READING_MANIFEST` through the `AUTH_*` steps.

---

## Helpers

`Tavstal.BeaconSDK.Helpers`

| Type | Notes |
| --- | --- |
| `GameHelper` (Domain) | `IsNewer`, `ParseVersionString`, `GetOfflinePlayerUUID` (and other helpers as available) |
| `ForgeMapper` (Domain) | Maps Forge library/version data |
| `FileSystemHelper` (IO) | Delete/move/compress, permissions, `CheckSHA1`/`CheckSHA256`, `GetFileHashAsync`, `GetFormattedSize`, `TempDir` |
| `OSHelper` (Platform) | `GetOperatingSystem`, `IsWindows11`, `IsArmBased`, `Is64BitOperatingSystem`, `GetRamInBytes`, home/desktop/programs directories, `OpenUrl`, `GetPlatformLibraryExtension` |
| `JsonHelper` (Serialization) | `ReadJsonFileAsync`, `WriteJsonFileAsync` |
| `LoggerHelper` | `DefaultLogFilePath`, `StartTime`, `EnqueueLog`, `ProcessLogQueueAsync` |

---

## Endpoints

`Tavstal.BeaconSDK.Models.Config.Endpoints`

Endpoint URLs are configured via `BeaconConfiguration.Endpoints` types (`MicrosoftEndpointConfig`, `MojangEndpointConfig`, `FabricEndpointConfig`, `QuiltEndpointConfig`, `ForgeEndpointConfig`, `NeoForgeEndpointConfig`). Use their properties and helpers like `MicrosoftEndpointConfig.MakeMicrosoftAuthUrl(...)`.
