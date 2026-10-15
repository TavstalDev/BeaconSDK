using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Helpers.Serialization;
using Tavstal.BeaconSDK.Models.Java;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Services.Implementations;

/// <inheritdoc />
public class JavaService : IJavaService
{
    private readonly IHttpService _httpService;
    private readonly IBeaconLogger _logger;

    private readonly Dictionary<EOperatingSystem, string[]> _lookupDirectories = new()
    {
        [EOperatingSystem.WINDOWS] =
        [
            @"C:\Program Files\Java",
            @"C:\Program Files (x86)\Java",
            @"C:\ProgramData\Oracle\Java",
            @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Java"
        ],
        [EOperatingSystem.LINUX] =
        [
            "/usr/lib/jvm",
            "/usr/java",
            "/opt/java",
            "/usr/local/java"
        ],
        [EOperatingSystem.MACOS] =
        [
            "/Library/Java/JavaVirtualMachines",
            "/System/Library/Java/JavaVirtualMachines"
        ]
    };

    private DateTime _cacheExpiration = DateTime.MinValue;
    private List<JavaVersion> _cachedJavaVersions = [];
    private JavaMirrorConfig? _mirrorConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JavaService" /> class.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostic messages, warnings, and errors related to Java operations.</param>
    /// <param name="httpService">HTTP service used to download Java distributions and related files.</param>
    public JavaService(IBeaconLogger<JavaService> logger, IHttpService httpService)
    {
        _logger = logger;
        _httpService = httpService;
    }

    /// <inheritdoc />
    public async Task<string?> DownloadJavaVersionAsync(int majorVersion, string targetPath, string mirrorsPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_mirrorConfig == null)
            {
                if (!File.Exists(mirrorsPath))
                {
                    _mirrorConfig = new JavaMirrorConfig();
                    await JsonHelper.WriteJsonFileAsync(mirrorsPath, _mirrorConfig,
                        BeaconJsonContext.Default.JavaMirrorConfig, cancellationToken);
                }
                else
                {
                    _mirrorConfig = await JsonHelper.ReadJsonFileAsync<JavaMirrorConfig>(mirrorsPath,
                                        BeaconJsonContext.Default.JavaMirrorConfig, cancellationToken) ??
                                    new JavaMirrorConfig();
                }
            }

            var operatingSystem = OSHelper.GetOperatingSystem();
            var isArmBased = OSHelper.IsArmBased();
            if (!_mirrorConfig.Mirrors.TryGetValue(operatingSystem, out var osMirrors))
            {
                _logger.LogWarning($"Failed to get java mirrors for {operatingSystem} OS.");
                return null;
            }

            var mirror = osMirrors.FirstOrDefault(x => x.Version == majorVersion && x.IsArm == isArmBased);
            if (mirror == null)
            {
                _logger.LogWarning(
                    $"No download URL found for Java {majorVersion} on {operatingSystem} OS {(isArmBased ? "arm" : "x64")}.");
                return null;
            }

            var tempDir = Path.Combine(FileSystemHelper.TempDir, "java");
            try
            {
                Directory.CreateDirectory(tempDir);
                var extension =  mirror.Url.EndsWith(".zip") ? "zip" : "tar.gz";
                var zipFilePath = Path.Combine(tempDir, $"java_{majorVersion}.{extension}");
                var downloadResult = await _httpService.DownloadFileAsync(mirror.Url, zipFilePath, mirror.Hash, progress, cancellationToken);
                if (!downloadResult)
                {
                    _logger.LogError($"Failed to download Java '{majorVersion}'.");
                    return null;
                }

                if (!File.Exists(zipFilePath))
                {
                    _logger.LogError($"Java download failed: {mirror.Url}");
                    return null;
                }

                if (extension == "tar.gz")
                {
                    await using Stream inStream = File.OpenRead(zipFilePath);
                    await using Stream gzipStream = new GZipInputStream(inStream);
                    using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream, Encoding.UTF8);
                    tarArchive.ExtractContents(targetPath);
                }
                else
                {
                    await ZipFile.ExtractToDirectoryAsync(zipFilePath, targetPath, cancellationToken);
                }

                string javaExecutablePath;
                if (OSHelper.GetOperatingSystem() != EOperatingSystem.WINDOWS)
                {
                    javaExecutablePath = Path.Combine(targetPath, "bin", "java");
                    if (!File.Exists(javaExecutablePath))
                    {
                        _logger.LogWarning($"Failed to get java executable from: {javaExecutablePath}");
                        return null;
                    }

                    if (!FileSystemHelper.MakeExecutable(javaExecutablePath))
                        _logger.LogError($"Failed to make java executable at: {javaExecutablePath}");
                }
                else
                {
                    javaExecutablePath = Path.Combine(targetPath, "bin", "javaw.exe");
                    if (!File.Exists(javaExecutablePath))
                    {
                        _logger.LogWarning($"Failed to get java executable from: {javaExecutablePath}");
                        return null;
                    }
                }

                return javaExecutablePath;
            }
            finally
            {
                FileSystemHelper.DeleteDirectory(tempDir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to download Java '{majorVersion}'.");
            _logger.LogCritical(ex.ToString());
            return null;
        }
    }

    /// <inheritdoc />
    public Task<string?> GetJavaVersionAsync(int majorVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = _cachedJavaVersions.FirstOrDefault(x => x.Major == majorVersion);
            return Task.FromResult(result?.Path);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to get Java version.");
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsJavaInstalledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "java", Arguments = " --version", RedirectStandardError = true, UseShellExecute = false
            };

            using var pr = Process.Start(psi);
            if (pr == null)
            {
                _logger.LogError("Failed to start Java process. Is Java installed?");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to validate Java:");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public async Task<JavaVersion?> GetJavaVersionDetailsAsync(string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "-XshowSettings:properties -version",
                RedirectStandardError = true, // Output goes to stderr
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return null;

            var output = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var majorVersion = string.Empty;
            var javaVersion = string.Empty;
            var architecture = string.Empty;

            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("java.specification.version ="))
                    majorVersion = line.Split('=')[1].Trim();

                if (line.Contains("java.version ="))
                    javaVersion = line.Split('=')[1].Trim();

                if (line.Contains("os.arch ="))
                    architecture = line.Split('=')[1].Trim();
            }

            if (majorVersion.StartsWith("1."))
            {
                var parts = majorVersion.Split('.');
                if (parts.Length > 1)
                {
                    majorVersion = parts[1];
                }
                else
                {
                    _logger.LogWarning($"Java version format '{majorVersion}' is unexpected, defaulting to 1.");
                    majorVersion = "1"; // Default to 1 if no version is found
                }
            }

            return new JavaVersion
            {
                Major = int.Parse(majorVersion), Version = javaVersion, Architecture = architecture, Path = path
            };
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to get Java version details:");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<JavaVersion>> LocateJavaInstallationsAsync(string? instanceJavaDir = null,
        bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (forceRefresh)
        {
            _cachedJavaVersions = [];
            _cacheExpiration = DateTime.MinValue;
        }

        if (_cachedJavaVersions.Count > 0 && _cacheExpiration > DateTime.Now)
            return _cachedJavaVersions;

        List<JavaVersion> javaVersions = [];
        var javaPaths = GetJavaPaths(OSHelper.GetOperatingSystem(), instanceJavaDir);

        foreach (var path in javaPaths)
        {
            var versionDetails = await GetJavaVersionDetailsAsync(path, cancellationToken);
            if (versionDetails == null)
                continue;

            javaVersions.Add(versionDetails);
        }

        _cachedJavaVersions = javaVersions;
        _cacheExpiration = DateTime.Now.AddMinutes(10); // Cache for 10 minutes

        return javaVersions;
    }

    /// <summary>
    ///     Builds a list of candidate Java executable paths by scanning the configured lookup directories
    ///     for the specified operating system.
    /// </summary>
    /// <param name="operatingSystem">The operating system whose Java installation directories should be searched.</param>
    /// <param name="instanceJavaDir">
    ///     Optional instance-specific Java directory to search before the default lookup
    ///     directories.
    /// </param>
    /// <returns>A list of full file paths to discovered Java executables. The list is empty if no installations are found.</returns>
    private List<string> GetJavaPaths(EOperatingSystem operatingSystem, string? instanceJavaDir = null)
    {
        List<string> paths = [];
        List<string> localDirs = [];
        if (!string.IsNullOrEmpty(instanceJavaDir))
            localDirs.Add(instanceJavaDir);
        localDirs.AddRange(_lookupDirectories[operatingSystem]);

        foreach (var dirPath in localDirs)
        {
            if (!Directory.Exists(dirPath))
                continue;

            var subDirs = Directory.GetDirectories(dirPath);
            foreach (var subDir in subDirs)
            {
                var javaPath = Path.Combine(subDir, "bin", GetJavaExecutableName(operatingSystem));
                if (!File.Exists(javaPath))
                    continue;

                paths.Add(javaPath);
            }
        }

        return paths;
    }

    /// <summary>
    ///     Gets the executable filename used to start Java for the specified operating system.
    /// </summary>
    /// <param name="operatingSystem">The operating system the executable name should match.</param>
    /// <returns><c>javaw.exe</c> on Windows; <c>java</c> on Linux, macOS, and unknown systems.</returns>
    private static string GetJavaExecutableName(EOperatingSystem operatingSystem)
    {
        switch (operatingSystem)
        {
            case EOperatingSystem.WINDOWS:
                return "javaw.exe";
            case EOperatingSystem.LINUX:
            case EOperatingSystem.MACOS:
            case EOperatingSystem.UNKNOWN:
            default:
                return "java";
        }
    }
}
