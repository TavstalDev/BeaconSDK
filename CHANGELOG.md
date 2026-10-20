# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
<details>
<summary>In development changes</summary>

</details>

## [1.0.0] - 2026-10-20
<details>
<summary>Initial release</summary>

This is the first published version. Interim development changes that were already
superseded along the way are omitted.

- **Authentication** - Microsoft OAuth2 device code flow and browser redirect flow
  (local `HttpListener` callback), Xbox Live / XSTS token exchange, Minecraft access
  token retrieval, game ownership checks and profile fetching/refresh. Refresh tokens
  are stored encrypted at rest through `IEncryptionService`.
- **Accounts & profiles** - `Account` model with encrypted access/refresh tokens,
  login refresh, and Mojang skin/cape management (change, upload, reset skins;
  show/hide capes) through `IMojangSkinService`.
- **Configuration** - a single `BeaconConfiguration` root (`Auth`, `DataProtection`,
  `Http`, `Endpoints`, `Logging`, `Libraries`) registered as a singleton by
  `AddBeaconServices`. All properties carry `JsonPropertyName` attributes so the
  configuration can be bound from JSON. `DataProtectionConfig` provides the key ring
  directory and per-platform fallback material.
- **Downloads** - everything is fetched through a shared named `HttpClient`
  (`default`) created via `IHttpClientFactory`, with gzip/deflate decompression,
  redirects, optional TLS 1.2 enforcement, retries on `429`/`408`, SHA-1 integrity
  checks, and configurable parallel download limits (`MaxParallelDownloads`,
  `BufferSize`, `TempFileSuffix`).
- **Manifests** - cached Mojang version manifest plus Fabric, Quilt, Forge and
  NeoForge loader manifests through `IManifestService`.
- **Instance installation** - `IInstanceInstallService` downloads and verifies the
  version JSON, client jar, assets (pre-1.6, legacy and modern layouts), libraries,
  natives, mappings and logging config, reports every stage through a single
  `IProgressReporter`, and populates the instance's argument builder. Pre-launch and
  post-exit commands, wrapper commands and environment variables are supported.
- **Mod loaders** - Fabric, Quilt, NeoForge and Forge across its whole history:
  early (up to 1.5.1), classic (1.5.2 - 1.7.2), legacy (1.7.10 - 1.12.x) and modern
  (1.12.2+) install flows, including the patched-jar flow for classic versions and
  the Forge processors for modern ones.
- **Support files** - the launch wrapper and the legacy Forge compatibility
  libraries are downloaded on demand from URLs configured in
  `BeaconConfiguration.Libraries` (`LibrariesConfig`/`FileEntry`) instead of being
  embedded in the assembly, so mirrors can be swapped without rebuilding the SDK.
- **Java management** - `IJavaService` locates installed JVMs, inspects their
  version/architecture, and downloads the required JDK from configurable mirrors.
- **Launching** - `IInstanceLaunchService` builds JVM and game arguments from the
  version metadata (including legacy argument formats), picks the classpath or a
  `classpath.txt` file for Java 9+, extracts natives, masks the access token in
  logs, starts the process and tracks/stops running instances.
- **Logging** - `IBeaconLogger<T>` with a configurable minimum level, console output
  (color-coded per level), optional forwarding to the host's `ILogger<T>`, and
  `ILogWriter`, a hosted background writer that appends entries to
  `LoggingConfig.DefaultLogFilePath` with per-path serialization (rotation is left
  to the application, see the example).
- **Progress reporting** - `IProgressReporter` with typed `EProgressKind` stages
  plus percentage/task-count updates, suitable for driving a UI.
- **DI & hosting** - `BeaconBootstrap.AddBeaconServices` registers every service as
  a singleton, the log writer as a hosted service, a `NullLogger<>` fallback, and
  the named `default` `HttpClient`.
- **Platform support** - Windows, Linux and macOS, with OS-specific library/natives
  handling, executable permissions and process control helpers.
- **Packaging** - targets .NET 10 with trimming and AOT analyzers enabled,
  source-generated `System.Text.Json` contexts, symbols (`.snupkg`) and embedded
  sources for debugging.
- **Documentation & samples** - getting started, authentication, installing &
  launching, Java management and API reference guides under `docs/`, a runnable
  `BeaconSDK.Example` (vanilla, Fabric, Quilt, Forge and NeoForge workers), and a
  unit test suite.

</details>


[unreleased]: https://github.com/TavstalDev/BeaconSDK/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/TavstalDev/BeaconSDK/releases/tag/v1.0.0
