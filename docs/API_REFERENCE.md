# API reference

A high-level map of the public API. Each type also ships with XML documentation in
the NuGet package.

- [Root namespace](#root-namespace)
- [Services](#services)
- [Instances](#instances)
- [Models](#models)
- [Enums](#enums)
- [Helpers](#helpers)
- [Configuration](#configuration)
- [Endpoints](#endpoints)

---

## Root namespace

`Tavstal.BeaconSDK`

### `BeaconBootstrap` (static)

| Member | Description |
| --- | --- |
| `IServiceCollection AddBeaconServices(this IServiceCollection, BeaconConfiguration configuration)` | Registers all SDK services. Also registers the `LogWriter` as a singleton, as `ILogWriter` and as a hosted service, a `NullLogger<>` fallback for `ILogger<>`, and a named `HttpClient` called `default` (via `AddHttpClient`) configured with the HTTP agent, accept header, timeout and handler settings |

`SetupLogger` and `BeaconBootstrap.LoggerTask` have been removed. The log writer is a
hosted service, so `host.RunAsync()` is all that is needed to start it.

---

## Services

`Tavstal.BeaconSDK.Services.Abstractions`

### `IHttpService`

`GetStringAsync` (with progress), `DownloadFileAsync`, `ParallelDownloadFilesAsync`.

Failures are reported through return values instead of exceptions: `GetStringAsync`
returns `null` and `DownloadFileAsync` returns `false`. GET requests retry on
`429 Too Many Requests` and `408 Request Timeout` (up to three attempts with a
three-second delay between them).

The implementation takes an `IHttpClientFactory` and resolves its client from the
named `default` client registered by `AddBeaconServices`; clients are no longer
created by `IHttpService` itself.

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

Downloads and prepares everything an instance needs: `DownloadVersionAsync`
(version JSON and client jar), `DownloadAssetsAsync`, `DownloadMappingsAsync`
(client mappings), `DownloadLoggingAsync` (logging config), `ExtractLaunchWrapperAsync`
(the launch wrapper downloaded from `LibrariesConfig.LaunchWrapper`) and `DownloadLibrariesAsync` (classpath, libraries and
natives). Registered in DI and used by `InstanceInstallService`; useful when you
need a subset of the install steps or want to control the classpath yourself.

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
`MicrosoftHttpAuthService`, `BeaconLogger`, `BeaconLogger<T>`, `LogWriter`.

`HttpService`, `MojangSkinService` and `MicrosoftAuthService` take an
`IHttpClientFactory` and create their clients through the named `default` client,
so all outbound HTTP requests share the configuration applied by `AddBeaconServices`.

---

## Instances

`Tavstal.BeaconSDK.Instances`

| Type | Description |
| --- | --- |
| `MinecraftInstance` | Base instance. Constructor takes `(id, BeaconConfiguration, InstanceConfiguration, IBeaconLogger, IProgressReporter?)`. Key members: `Id`, `Config`, `VersionData`, `MinecraftVersionMeta`, `ArgumentBuilder`, `GetCombinedLibraries`, `InstallModdedAsync`, `GetVersionName`. |
| `FabricInstance`, `QuiltInstance` | Modded instances with constructor `(id, BeaconConfiguration, InstanceConfiguration, IBeaconLogger, IProgressReporter?)`. |
| `NeoForgeInstance` | Constructor `(id, BeaconConfiguration, InstanceConfiguration, IBeaconLogger, IProgressReporter?)`. Derives from `ForgeInstanceBase`. |
| `ForgeInstance` (static) | `GetForgeInstance(...)` returns the right Forge implementation. `GetLegacyLibraries(string mcVersion)` returns the file names of the legacy Forge compatibility libraries for Minecraft 1.3 – 1.5 (an empty list for other versions); each name is a key of `BeaconConfiguration.Libraries.Forge`, which provides the download URL. |
| `ForgeEarlyInstance`, `ForgeClassicInstance`, `ForgeLegacyInstance`, `ForgeModernInstance` | Forge implementations for each era. `ForgeEarlyInstance` and `ForgeClassicInstance` take two extra leading arguments (`forgeVersionName`, `universalName` / `universalFormat`) before `id`; `ForgeLegacyInstance` takes one (`forgeVersionName`); `ForgeModernInstance` matches the base shape. |
| `Forge/ForgeInstanceBase` | Shared Forge/NeoForge install logic. |

---

## Models

`Tavstal.BeaconSDK.Models`

| Type | Notes |
| --- | --- |
| `IProgressReporter` | `Report(double)`, `Report(double, string, bool)`, `Report(EProgressKind, double)`, `ReportExpectedTask(int?)`, `ReportTaskCompleted()` |
| `DownloadEntry` | `Url`, `Path`, `Sha1`, `Progress` (an `IProgress<double>` built from an optional `IProgressReporter`) |
| `FileEntry` | A configured remote file: `Url` (download location; an empty value disables the download) and optional `Sha1` used to verify an existing local copy |
| `EProgressKind` | Progress-stage enum (see Enums) |
| `LockEntry` | Semaphore plus reference count used by `LogWriter` and `JsonHelper` for file locking |

`Tavstal.BeaconSDK.Models.Instance.Config`

| Type | Notes |
| --- | --- |
| `GameDetails` | Java path, memory (MB), JVM args, version, kind, custom version/dir, pre/post commands, wrappers, env vars, server |
| `PathDetails` | Assets/cache/libraries/versions dirs, custom natives |
| `LauncherDetails` | `LauncherName`, `LauncherVersion` |
| `ClientDetails` | `AccessToken`, `DisplayName`, `UUID`, `ClientId`, `Xuid`, `IsOffline` |
| `LaunchArg` | A launch argument: `Arg` (text) and `Priority` (ordering weight) |

`Tavstal.BeaconSDK.Models.Instance`

| Type | Notes |
| --- | --- |
| `InstanceConfiguration` | Aggregates version, game, path, launcher and client config, plus an optional `Resolution` |
| `ModdedData` | `MainClass`, `Libraries` |
| `ArgumentBuilder` | Builds JVM and game argument strings. Adds `-Xms`/`-Xmx` from `GameDetails.MinMemory`/`MaxMemory` (MB) |
| `VersionDetails` | Resolved vanilla/custom version paths, `GameDir` and `NativesDir` |
| `Resolution` | Window size as `X` / `Y` (uint) |

`Tavstal.BeaconSDK.Models.Accounts`

| Type | Notes |
| --- | --- |
| `Account` | `Id`, `Uuid`, `DisplayName`, `Type`, `AccessTokenExpireDate`, `Skins`, `MojangProfile`, `CanExpire`; `Get/SetAccessToken`, `Get/SetRefreshToken` |
| `AccountSkin` | Skin/cape metadata |

`Tavstal.BeaconSDK.Models.Java`

`JavaVersion` (`Major`, `Version`, `Architecture`, `Path`), `JavaMirrorConfig`
(`Mirrors`, keyed by `EOperatingSystem`), `JavaMirror` (`Version`, `IsArm`, `Url`,
optional `Hash` for integrity verification).

`Tavstal.BeaconSDK.Models.Logging`

| Type | Notes |
| --- | --- |
| `LogEntry` | Record `(string LogLevel, string ModuleName, string? Message)` |
| `BeaconLoggerExtensions` | `LogDebug`, `LogTrace`, `LogInformation`, `LogWarning`, `LogError`, `LogCritical`, `Log` (each with an optional leading `Exception`). Declared as C# `extension` blocks over `IBeaconLogger` |

The logger contracts and implementations live with the other services:

| Type | Notes |
| --- | --- |
| `Abstractions/IBeaconLogger` | `Log(LogLevel, LogEntry, Exception?, Func<LogEntry, Exception?, string>)`, `ModuleName`, `LogLevel` (both get-only properties, not methods) |
| `Abstractions/IBeaconLogger<T>` | Marker interface over `IBeaconLogger`, resolved by DI as a singleton |
| `Abstractions/ILogWriter` | `EnqueueLog(string entry, string? logFilePath = null)`. Returns immediately; the implementation buffers and writes on a background loop |
| `Implementations/BeaconLogger` | Constructor `(string moduleName, LogLevel logLevel, bool printLogs = true, string? customLogFilePath = null, ILogWriter? logWriter = null, ILogger? logger = null)`. Discards entries below `logLevel`, writes the rest to `logger` when supplied or to the console (color-coded per level), and always forwards them to `logWriter` when one is supplied |
| `Implementations/BeaconLogger<T>` | Adds the DI constructor `(ILogWriter? logWriter, ILogger<T>? logger, BeaconConfiguration config)`, which takes the level from `config.Logging.LogLevel`, the file from `config.Logging.DefaultLogFilePath`, and only forwards to `logger` when `config.Logging.EnableCustomLogger` is false. `ModuleName` is `typeof(T).Name` |
| `Implementations/LogWriter` | `IHostedService` + `ILogWriter`. `StartAsync` spawns the drain loop; entries are buffered per target file and appended with `File.AppendAllTextAsync`, creating parent directories as needed. Writes are serialized per path. An entry with no path and an empty `Logging.DefaultLogFilePath` is discarded |

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
| `GameHelper` (Domain) | `isNewer(versionA, versionB)`, `ParseVersionString`, `GetOfflinePlayerUUID` |
| `ForgeMapper` (Domain) | `Map`, `Interpolation`, `ToFullPath`, `HandleEmptyArg` for legacy Forge arguments |
| `FileSystemHelper` (IO) | Delete/move/compress, permissions and free space, `MakeExecutable`/`MakeFileReadonly`/`MakeFileWritable`, `CheckSHA1`/`CheckSHA256`/`CheckByDigest`, `GetFileHashAsync`, `GetContentHash`, `GetFormattedSize`, `IsFileLocked`, `IsSafeToDelete`, `OpenFolderInFileExplorer`, `TempDir` |
| `OSHelper` (Platform) | `GetOperatingSystem`, `IsWindows11`, `IsArmBased`, `Is64BitOperatingSystem`, `GetDedicatedGpuType`, `GetRamInBytes`, home/desktop/programs directories, `OpenUrl`, `GetPlatformLibraryExtension` |
| `ProcessHelper` (Platform) | `Start(ProcessStartInfo, onDataReceived?, errorDataReceived?)` shared process start helper |
| `JsonHelper` (Serialization) | `ReadJsonFile`/`ReadJsonFileAsync`, `WriteJsonFile`/`WriteJsonFileAsync` (all take a `JsonTypeInfo<T>`) |

---

## Configuration

`Tavstal.BeaconSDK.Models.Config`

`BeaconConfiguration` is registered as a singleton by `AddBeaconServices`, so any
SDK service can resolve it. `Auth` is the only required member.

| Type | Notes |
| --- | --- |
| `BeaconConfiguration` | `Auth` (required), `DataProtection`, `Http`, `Endpoints`, `Logging`, `Libraries` |
| `LibrariesConfig` | `LaunchWrapper` (a `FileEntry` for the launch wrapper jar), `Forge` (a `Dictionary<string, FileEntry>` of legacy Forge libraries keyed by file name) |
| `LoggingConfig` | `LogLevel` (default `Information`) sets the minimum level for DI-created loggers; `EnableCustomLogger` (default `true`) keeps output on the console, set it to `false` to forward to the host's `ILogger<T>` instead; `DefaultLogFilePath` (default `null`) is the file entries are appended to, and `null` disables file logging |
| `AuthConfiguration` | `MicrosoftClientId` (required, may be `null`/empty only if Microsoft login is unused), `DeviceCodePollInterval` (`TimeSpan`, defaults to 5 seconds; the SDK does not read it, pass it to `IMicrosoftDeviceAuthService.StartListeningAsync` yourself) |
| `HttpConfig` | `Agent`, `AcceptHeader`, `Timeout`, `AutomaticDecompression`, `AllowAutoRedirect`, `EnableMultipleHttp2Connections`, `MaxParallelDownloads`, `BufferSize`, `TempFileSuffix`, `UseTls12Protocol` |
| `DataProtectionConfig` | `KeysDirectory` (default `keysDir`), `ApplicationName` (default `myApplication`), `ProtectorPurpose` (default `BeaconSDK.Encryption.v1`), and the per-platform fallback material `WindowsEntropy`, `LinuxKey`, `MacKey` |
| `EndpointConfig` | `Microsoft`, `Mojang`, `Fabric`, `Quilt`, `Forge`, `NeoForge` |

Every configuration property carries a `JsonPropertyName` attribute, so the
configuration types round-trip through `System.Text.Json` with your own context.
The SDK's own source-generated contexts (`BeaconJsonContext`, `CoreJsonContext`,
`GenericJsonContext`) cover accounts, Java mirrors, manifests and Mojang metadata,
not `BeaconConfiguration`.

---

## Endpoints

`Tavstal.BeaconSDK.Models.Config.Endpoints`

Endpoint URLs are configured via `BeaconConfiguration.Endpoints`. Placeholders such as
`{0}` / `{1}` are filled with `string.Format` at request time.

| Type | Properties |
| --- | --- |
| `MicrosoftEndpointConfig` | `AuthUrl`, `TokenUrl`, `DeviceUrl`, `DeviceTokenUrl`, `XboxAuthUrl`, `XboxXstsUrl`; helper `MakeMicrosoftAuthUrl(clientId, redirectUrl)` builds the browser-flow authorization URL |
| `MojangEndpointConfig` | `AuthUrl`, `ProfileUrl`, `OwnershipUrl`, `MinecraftManifestUrl`, `MinecraftResourcesUrl`, `PlayerConfigUrl` |
| `FabricEndpointConfig` | `VersionManifestUrl`, `LoaderJsonUrl`, `LoaderJarUrl` |
| `QuiltEndpointConfig` | `VersionManifestUrl`, `LoaderJsonUrl`, `LoaderJarUrl` |
| `ForgeEndpointConfig` | `VersionManifest`, `LoaderUniversalZipUrl`, `InstallerJarUrl` |
| `NeoForgeEndpointConfig` | `VersionManifest`, `InstallerJarUrl` |
