# Java management

`IJavaService` finds Java runtimes already installed on the machine, reports their
version and architecture, and can download a JDK when one is missing.

A Minecraft version declares the Java major version it needs in
`VersionMeta.JavaVersionMeta?.MajorVersion` (defaults to `8` when absent).

---

## Locate installed runtimes

```csharp
await java.LocateJavaInstallationsAsync(instanceJavaDir: "game/java", cancellationToken: ct);
```

Scan order:

1. `instanceJavaDir` (if provided), so SDK-downloaded runtimes are found first.
2. Platform defaults:

   | Platform | Directories |
   | --- | --- |
   | Windows | `C:\Program Files\Java`, `C:\Program Files (x86)\Java`, `C:\ProgramData\Oracle\Java`, `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Java` |
   | Linux | `/usr/lib/jvm`, `/usr/java`, `/opt/java`, `/usr/local/java` |
   | macOS | `/Library/Java/JavaVirtualMachines`, `/System/Library/Java/JavaVirtualMachines` |

Results are cached for 10 minutes. Pass `forceRefresh: true` to bypass the cache.

Each discovered runtime is inspected by running
`<java> -XshowSettings:properties -version` and parsing the major version, full
version string and `os.arch`.

---

## Query runtimes

```csharp
// Path of an installed runtime for a given major version (or null)
string? path = await java.GetJavaVersionAsync(majorVersion: 21, ct);

// Detailed information for a specific executable
JavaVersion? details = await java.GetJavaVersionDetailsAsync("/usr/lib/jvm/temurin-21/bin/java", ct);

// Is a system "java" on the PATH runnable?
bool installed = await java.IsJavaInstalledAsync(ct);
```

`JavaVersion` exposes `Major`, `Version`, `Architecture` and `Path`.

---

## Download a runtime

When no suitable runtime is found, download one:

```csharp
var progress = new Progress<double>(p => Console.WriteLine($"Downloading Java {p:0.00}%"));

var javaPath = await java.DownloadJavaVersionAsync(
    majorVersion: 21,
    targetPath: "game/java",
    mirrorsPath: "java-mirrors.json",
    progress: progress,
    cancellationToken: ct);
```

Notes:

- The built-in `JavaMirrorConfig` ships Temurin builds for major versions
  **8, 16, 17, 21 and 25**. Any other version returns `null` unless you add a
  mirror entry for it.
- The mirror is picked by OS **and** by `OSHelper.IsArmBased()`, so a machine whose
  architecture has no entry for that version returns `null`.
- Archives are `.zip` (Windows) or `.tar.gz` (Linux/macOS); both are extracted to
  `targetPath`.
- On non-Windows platforms the extracted `bin/java` is marked executable.
- On success the method returns the path to `javaw.exe` (Windows) or `bin/java`
  (Linux/macOS); it returns `null` if the version is unsupported, no URL is
  configured or the download fails.

### Mirror configuration

Download URLs come from a JSON mirror config. If `mirrorsPath` does not exist, a
default `JavaMirrorConfig` is created and written there. Each `JavaMirror` entry
declares a major version, whether it targets ARM (`IsArm`), a download `Url` and
an optional `Hash` used for integrity verification:

```
JavaMirrorConfig
└── Mirrors (per EOperatingSystem)
    ├── Windows ─ 8, 16, 17, 21, 25 (x86_64)
    ├── Linux   ─ 8, 16, 17, 21, 25 (x86_64)
    └── Mac     ─ 8, 16 (x86_64); 17, 21, 25 (x86_64 + ARM)
```

Edit this file to point at your own mirrors or to add versions that are not
built in.

---

## Recommended flow

```csharp
var major = instance.MinecraftVersionMeta.JavaVersionMeta?.MajorVersion ?? 8;

await java.LocateJavaInstallationsAsync("game/java", cancellationToken: ct);

var javaPath = await java.GetJavaVersionAsync(major, ct);
if (string.IsNullOrEmpty(javaPath))
    javaPath = await java.DownloadJavaVersionAsync(major, "game/java", "java-mirrors.json", progress, ct);

if (string.IsNullOrEmpty(javaPath))
    throw new InvalidOperationException($"Could not provide Java {major}.");

instance.Config.Game.JavaPath = javaPath;
```
