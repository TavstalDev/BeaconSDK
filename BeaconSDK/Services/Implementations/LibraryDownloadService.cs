using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Domain;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Endpoints;
using Tavstal.BeaconSDK.Models.Installer;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.MojangApi;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Services.Abstractions;
using CoreJsonContext = Tavstal.BeaconSDK.Models.Json.CoreJsonContext;

namespace Tavstal.BeaconSDK.Services.Implementations;

/// <inheritdoc />
public class LibraryDownloadService : ILibraryDownloadService
{
    private const int MaxParallelDownloads = 4;
    private readonly IHttpService _httpService;
    private readonly ICustomLogger _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LibraryDownloadService" /> class.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics and errors during library download operations.</param>
    /// <param name="httpService">HTTP service used to download files and fetch remote content.</param>
    public LibraryDownloadService(ICustomLogger<LibraryDownloadService> logger, IHttpService httpService)
    {
        _logger = logger;
        _httpService = httpService;
    }

    /// <inheritdoc />
    public async Task<VersionMeta?> DownloadVersionAsync(VersionDetails versionData, MinecraftVersion minecraftVersion,
        IProgressReporter? progressReporter = null, CancellationToken cancellationToken = default)
    {
        // JSON
        var versionResult = await DownloadAndSaveFileAsync<VersionMeta>(
            versionData.VanillaJsonPath,
            minecraftVersion.Url,
            string.Empty,
            progressReporter,
            CoreJsonContext.Default.VersionMeta, cancellationToken);

        if (versionResult == null) return null;

        // JAR
        await DownloadAndSaveBinaryFileAsync(
            versionData.VanillaJarPath,
            versionResult.Downloads.Client.Url,
            string.Empty, // allow modified jar usage
            progressReporter,
            cancellationToken);

        // Create default JavaVersionMeta if null
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
        if (versionResult.JavaVersionMeta == null)
            versionResult.JavaVersionMeta = new JavaVersionMeta { MajorVersion = 8 };

        return versionResult;
    }

    /// <inheritdoc />
    public async Task DownloadAssetsAsync(VersionMeta versionMeta, string assetsDir, string gameDir,
        IProgressReporter? progressReporter = null, CancellationToken cancellationToken = default)
    {
        // AssetIndex
        var assetIndexId = versionMeta.Index.Id;
        var assetPath = Path.Combine(assetsDir, $"indexes/{assetIndexId}.json");

        var resultJson = await DownloadAndSaveFileAsync(
            assetPath,
            versionMeta.Index.Url,
            versionMeta.Index.Sha1,
            progressReporter,
            cancellationToken);

        if (resultJson == null) return;

        var assetsType = versionMeta.Assets;

        using var doc = JsonDocument.Parse(resultJson);
        if (!doc.RootElement.TryGetProperty("objects", out var assetJToken) ||
            assetJToken.ValueKind != JsonValueKind.Object)
            throw new Exception(
                "Asset JToken is null or invalid, something went wrong while reading the asset index JSON.");

        // Assets
        progressReporter?.Report(EProgressKind.INSTANCE_READING_ASSETS, 0);

        var semaphore = new SemaphoreSlim(MaxParallelDownloads);
        long downloadedBytes = 0;
        var tasks = new List<Task>();

        switch (assetsType)
        {
            // Olds Assets
            case "pre-1.6":
            {
                var resourcesDir = Path.Combine(gameDir, "resources");
                Directory.CreateDirectory(resourcesDir);

                // For some reason pre-1.6 still wants to use icons from legacy folder
                // So we fix this by copying the icon files to the legacy folder
                var legacyDir = Path.Combine(assetsDir, "virtual", "legacy");
                Directory.CreateDirectory(legacyDir);

                foreach (var token in assetJToken.EnumerateObject())
                {
                    if (!token.Value.TryGetProperty("hash", out var hashElement)) continue;
                    var hash = hashElement.GetString();
                    if (string.IsNullOrEmpty(hash)) continue;

                    var rawFilePath = token.Name;

                    var fileName = Path.GetFileName(rawFilePath);
                    var fileDirectory = Path.GetDirectoryName(rawFilePath);
                    string? objectDir = null;
                    if (!string.IsNullOrEmpty(fileDirectory))
                    {
                        objectDir = Path.Combine(resourcesDir, fileDirectory);
                        Directory.CreateDirectory(objectDir);
                    }

                    var objectPath = Path.Combine(objectDir ?? resourcesDir, fileName);
                    if (File.Exists(objectPath))
                        continue;

                    await semaphore.WaitAsync(cancellationToken);
                    var t = Task.Run(async () =>
                    {
                        try
                        {
                            await _httpService.DownloadFileAsync(
                                $"{MicrosoftEndpoints.MinecraftResourcesUrl}/{hash[..2]}/{hash}",
                                objectPath,
                                null, cancellationToken);

                            if (fileName.Contains("icon") || (objectDir != null && objectDir.Contains("icon")))
                            {
                                if (!string.IsNullOrEmpty(fileDirectory))
                                {
                                    objectDir = Path.Combine(legacyDir, fileDirectory);
                                    Directory.CreateDirectory(objectDir);
                                }

                                var legacyObjectPath = Path.Combine(objectDir ?? legacyDir, fileName);
                                if (!File.Exists(legacyObjectPath) &&
                                    File.Exists(objectPath)) // Double check to be sure
                                    File.Copy(objectPath, legacyObjectPath);
                            }

                            var size = 0;
                            if (token.Value.TryGetProperty("size", out var sizeElement))
                                sizeElement.TryGetInt32(out size);

                            Interlocked.Add(ref downloadedBytes, size);

                            var percent = downloadedBytes / (double)versionMeta.Index.TotalSize * 100d;
                            progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_ASSETS, percent);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken);
                    tasks.Add(t);
                }

                break;
            }
            // Legacy Assets
            case "legacy":
            {
                var resourcesDir = Path.Combine(assetsDir, "virtual", "legacy");
                Directory.CreateDirectory(resourcesDir);

                foreach (var token in assetJToken.EnumerateObject())
                {
                    if (!token.Value.TryGetProperty("hash", out var hashElement)) continue;
                    var hash = hashElement.GetString();
                    if (string.IsNullOrEmpty(hash)) continue;

                    var rawFilePath = token.Name;

                    var fileName = Path.GetFileName(rawFilePath);
                    var fileDirectory = Path.GetDirectoryName(rawFilePath);
                    string? objectDir = null;
                    if (!string.IsNullOrEmpty(fileDirectory))
                    {
                        objectDir = Path.Combine(resourcesDir, fileDirectory);
                        Directory.CreateDirectory(objectDir);
                    }

                    var objectPath = Path.Combine(objectDir ?? resourcesDir, fileName);
                    if (File.Exists(objectPath))
                        continue;

                    await semaphore.WaitAsync(cancellationToken);
                    var t = Task.Run(async () =>
                    {
                        try
                        {
                            await _httpService.DownloadFileAsync(
                                $"{MicrosoftEndpoints.MinecraftResourcesUrl}/{hash[..2]}/{hash}",
                                objectPath,
                                null, cancellationToken);

                            var size = 0;
                            if (token.Value.TryGetProperty("size", out var sizeElement))
                                sizeElement.TryGetInt32(out size);

                            Interlocked.Add(ref downloadedBytes, size);

                            var percent = downloadedBytes / (double)versionMeta.Index.TotalSize * 100d;
                            progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_ASSETS, percent);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken);
                    tasks.Add(t);
                }

                break;
            }
            // Modern Assets
            default:
            {
                // Asset Dir
                var assetObjectDir = Path.Combine(assetsDir, "objects");
                Directory.CreateDirectory(assetObjectDir);

                // Modern structure in Minecraft asset index is also a dictionary of properties, same as pre-1.6/legacy
                foreach (var token in assetJToken.EnumerateObject())
                {
                    if (!token.Value.TryGetProperty("hash", out var hashElement)) continue;
                    var hash = hashElement.GetString();
                    if (string.IsNullOrEmpty(hash)) continue;

                    var objectDir = Path.Combine(assetObjectDir, hash[..2]);
                    var objectPath = Path.Combine(objectDir, $"{hash}");

                    Directory.CreateDirectory(objectDir);

                    if (File.Exists(objectPath))
                        continue;

                    await semaphore.WaitAsync(cancellationToken);
                    var t = Task.Run(async () =>
                    {
                        try
                        {
                            await _httpService.DownloadFileAsync(
                                $"{MicrosoftEndpoints.MinecraftResourcesUrl}/{hash[..2]}/{hash}",
                                objectPath,
                                null, cancellationToken);

                            var size = 0;
                            if (token.Value.TryGetProperty("size", out var sizeElement))
                                sizeElement.TryGetInt32(out size);

                            Interlocked.Add(ref downloadedBytes, size);

                            var percent = downloadedBytes / (double)versionMeta.Index.TotalSize * 100d;
                            progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_ASSETS, percent);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken);
                    tasks.Add(t);
                }

                break;
            }
        }

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async Task<LaunchArg?> DownloadLoggingAsync(VersionMeta versionMeta, string versionDirectory, string gameDir,
        IProgressReporter? progressReporter = null, CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ArrangeNullCheckingPattern
        if (versionMeta.LoggingMeta is not { }) return null;

        var logFilePath = Path.Combine(versionDirectory, versionMeta.LoggingMeta.Client.File.Id);

        var logContent = await DownloadAndSaveFileAsync(
            logFilePath,
            versionMeta.LoggingMeta.Client.File.Url,
            string.Empty,
            progressReporter,
            cancellationToken);

        if (logContent == null) return null;

        // FIX LOG LOCATION
        var modifiedContent = logContent
            .Replace("fileName=\"logs", $"fileName=\"{gameDir}/logs")
            .Replace("filePattern=\"logs", $"filePattern=\"{gameDir}/logs");

        await File.WriteAllTextAsync(logFilePath, modifiedContent, cancellationToken);

        return new LaunchArg(versionMeta.LoggingMeta.Client.Argument.Replace("${path}", logFilePath), 2);
    }

    /// <inheritdoc />
    public async Task DownloadMappingsAsync(VersionMeta versionMeta, VersionDetails versionData,
        IProgressReporter? progressReporter = null, CancellationToken cancellationToken = default)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (versionMeta.Downloads.ClientMappings == null) return;

        var clientMappinsPath = Path.Combine(versionData.VanillaVersionDirectory, "client.txt");

        await DownloadAndSaveFileAsync(
            clientMappinsPath,
            versionMeta.Downloads.ClientMappings.Url,
            versionMeta.Downloads.ClientMappings.Sha1,
            progressReporter,
            cancellationToken); // Deserialize to string
    }

    /// <inheritdoc />
    public async Task<string?> ExtractLaunchWrapperAsync(string libsDir, CancellationToken cancellationToken = default)
    {
        var targetDir = Path.Combine(libsDir, "io", "github", "tavstaldev", "launchWrapper");
        Directory.CreateDirectory(targetDir);

        const string targetAssetName = "launchWrapper-1.0.jar";
        var targetFile = Path.Combine(targetDir, targetAssetName);
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream("Tavstal.BeaconSDK.Assets.launchWrapper-1.0.jar");
        if (stream == null)
            throw new Exception(
                "Failed to find embedded resource: Tavstal.BeaconSDK.Assets.launchWrapper-1.0.jar");

        if (File.Exists(targetFile))
        {
            var existingHash = await FileSystemHelper.GetFileHashAsync(targetFile);
            var expectedHash = await FileSystemHelper.GetFileHashAsync(stream);
            if (existingHash == expectedHash && expectedHash != null)
                return targetFile;
            FileSystemHelper.DeleteFile(targetFile);
        }

        await using var fileStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, cancellationToken);
        return targetFile;
    }

    /// <inheritdoc />
    public async Task<List<string>> DownloadLibrariesAsync(EMinecraftKind kind, VersionDetails versionData,
        List<LibraryMeta> mcLibs, List<string> classPath,
        string cacheDir, string libsDir, IProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        progressReporter?.Report(EProgressKind.INSTANCE_READING_LIBRARIES, 0);

        var safeClassPath = new ConcurrentBag<string>(classPath);
        var jsonKey = $"{versionData.MinecraftVersion}-{kind}-{versionData.CustomVersion}";
        var librarySizeCacheFilePath = Path.Combine(cacheDir, "libsizes.json");
        JsonObject cacheObject;
        if (!File.Exists(librarySizeCacheFilePath)) // Create empty cache file if it does not exist
        {
            cacheObject = new JsonObject();
            await File.WriteAllTextAsync(librarySizeCacheFilePath, "{}", cancellationToken);
        }
        else
        {
            var json = await File.ReadAllTextAsync(librarySizeCacheFilePath, cancellationToken);
            cacheObject = JsonNode.Parse(json)?.AsObject()!;
        }

        // Calculate or read library size
        long overallLibrarySize;
        if (cacheObject.TryGetPropertyValue(jsonKey, out var cacheValue) && cacheValue != null)
        {
            overallLibrarySize = long.Parse(cacheValue.ToString());
        }
        else
        {
            overallLibrarySize = mcLibs
                .Where(lib => lib.GetRulesResult() && lib.Downloads.Artifact != null)
                .Sum(lib => lib.Downloads.Artifact?.Size ?? 0);

            cacheObject[jsonKey] = overallLibrarySize;
            await File.WriteAllTextAsync(librarySizeCacheFilePath, cacheObject.ToString(), cancellationToken);
        }

        var semaphore = new SemaphoreSlim(MaxParallelDownloads);
        long downloadedBytes = 0;
        var tasks = new List<Task>();

        // Download libraries
        // Before downloading, we must get rid of duplicates
        // Fixes fabric 0.17.x libraries issue
        var libraryMetas = mcLibs.Where(lib => lib.GetRulesResult()).ToArray();
        foreach (var lib in libraryMetas)
        {
            var libParts = lib.Name.Split(':').ToList();
            var libVersion = libParts[2];
            libParts.RemoveAt(2);
            var libName = string.Join(":", libParts);
            var hasNewerVersion = libraryMetas.Any(otherLib =>
            {
                var otherParts = otherLib.Name.Split(':').ToList();
                if (otherParts.Count < 3) return false;

                var otherVersion = otherParts[2];
                otherParts.RemoveAt(2);
                var otherName = string.Join(":", otherParts);
                return otherName == libName && GameHelper.isNewer(otherVersion, libVersion);
            });
            if (hasNewerVersion)
                continue;

            await semaphore.WaitAsync(cancellationToken);
            var t = Task.Run(async () =>
            {
                try
                {
                    if (lib.Downloads.Artifact != null)
                    {
                        var libFilePath =
                            await DownloadLibraryArtifactAsync(lib, libsDir, progressReporter, cancellationToken);
                        Interlocked.Add(ref downloadedBytes, lib.Downloads.Artifact.Size);

                        if (!string.IsNullOrEmpty(libFilePath) && !safeClassPath.Contains(libFilePath))
                            safeClassPath.Add(libFilePath);
                    }

                    if (lib.Downloads.Classifiers != null)
                    {
                        var classifier = lib.Downloads.Classifiers.GetOsNative();
                        var libJarFilePath = Path.Combine(libsDir, classifier.Path);
                        await DownloadNativeFileAsync(classifier.Url, libJarFilePath, versionData.NativesDir,
                            progressReporter, cancellationToken);
                        Interlocked.Add(ref downloadedBytes, classifier.Size);

                        if (!string.IsNullOrEmpty(libJarFilePath) && !safeClassPath.Contains(libJarFilePath))
                            safeClassPath.Add(libJarFilePath);
                    }
                }
                finally
                {
                    progressReporter?.Report(downloadedBytes / (double)overallLibrarySize * 100d);
                    semaphore.Release();
                    progressReporter?.ReportTaskCompleted();
                }
            }, cancellationToken);
            tasks.Add(t);
        }

        progressReporter?.ReportExpectedTask(tasks.Count);
        await Task.WhenAll(tasks);
        progressReporter?.ReportExpectedTask(null);
        return safeClassPath.ToList();
    }

    /// <summary>
    ///     Downloads a library artifact to the local libraries directory if it is not already present.
    /// </summary>
    /// <param name="lib">The library metadata describing the artifact to download.</param>
    /// <param name="libsDir">The root libraries directory where the artifact should be stored.</param>
    /// <param name="progressReporter">Optional progress reporter used to report download status.</param>
    /// <param name="cancellationToken">Cancellation token observed during the download operation.</param>
    /// <returns>
    ///     A task that resolves to the local file path of the downloaded artifact, or an empty string
    ///     if the library does not contain an artifact.
    /// </returns>
    private async Task<string> DownloadLibraryArtifactAsync(LibraryMeta lib, string libsDir,
        IProgressReporter? progressReporter,
        CancellationToken cancellationToken = default)
    {
        if (lib.Downloads.Artifact == null)
            return string.Empty;

        var localPath = lib.Downloads.Artifact.Path;
        var libDirPath = Path.Combine(libsDir, Path.GetDirectoryName(localPath)!);
        Directory.CreateDirectory(libDirPath);

        var libFilePath = Path.Combine(libsDir, localPath);
        if (!File.Exists(libFilePath) && !string.IsNullOrEmpty(lib.Downloads.Artifact.Url))
        {
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_LIBRARIES, e);
            };

            await _httpService.DownloadFileAsync(lib.Downloads.Artifact.Url, libFilePath, progress, cancellationToken);
        }

        return libFilePath;
    }

    /// <summary>
    ///     Downloads a native library file and extracts its native contents into the specified native directory.
    /// </summary>
    /// <param name="url">The URL of the native archive to download.</param>
    /// <param name="filePath">The local path where the downloaded native archive should be stored.</param>
    /// <param name="nativeDir">The directory where extracted native files should be placed.</param>
    /// <param name="progressReporter">Optional progress reporter used to report download status.</param>
    /// <param name="cancellationToken">Cancellation token observed during the download operation.</param>
    /// <returns>A task that completes when the native file has been downloaded and extracted.</returns>
    private async Task DownloadNativeFileAsync(string url, string filePath, string nativeDir,
        IProgressReporter? progressReporter, CancellationToken cancellationToken = default)
    {
        try
        {
            var libDir = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(libDir);

            if (File.Exists(filePath))
            {
                ExtractNativeFiles(filePath, nativeDir);
                return;
            }

            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                progressReporter?.Report(EProgressKind.INSTANCE_DOWNLOADING_NATIVES, e);
            };

            await _httpService.DownloadFileAsync(url, filePath, progress, cancellationToken);
            ExtractNativeFiles(filePath, nativeDir);
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(filePath))
                FileSystemHelper.DeleteFile(filePath);
            throw;
        }
    }

    /// <summary>
    ///     Downloads a file from a URL, deserializes its content, and saves it locally if it doesn't already exist.
    /// </summary>
    /// <typeparam name="T">The type to which the file content will be deserialized.</typeparam>
    /// <param name="filePath">The local file path where the file will be saved.</param>
    /// <param name="url">The URL from which the file will be downloaded.</param>
    /// <param name="sha1">
    ///     The expected SHA1 hash of the file for integrity verification. If null or empty, the hash check is
    ///     skipped.
    /// </param>
    /// <param name="progressReporter">An optional progress reporter for tracking download progress.</param>
    /// <param name="jsonTypeInfo">A JSON type information provider for customizing JSON serialization behavior.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The deserialized object of type <typeparamref name="T" /> or null if the operation fails.</returns>
    private async Task<T?> DownloadAndSaveFileAsync<T>(string filePath, string url, string sha1,
        IProgressReporter? progressReporter, JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(filePath) && (string.IsNullOrEmpty(sha1) || FileSystemHelper.CheckSHA1(filePath, sha1)))
            {
                progressReporter?.Report(0, Path.GetFileName(filePath), false);
                var jsonResult = await File.ReadAllTextAsync(filePath, cancellationToken);
                return JsonSerializer.Deserialize(jsonResult, jsonTypeInfo);
            }

            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                progressReporter?.Report(e, Path.GetFileName(filePath));
            };

            var result = await _httpService.GetStringAsync(url, progress, cancellationToken);
            if (result == null)
                return default;

            var deserializedResult = JsonSerializer.Deserialize(result, jsonTypeInfo);
            if (deserializedResult != null)
            {
                var fileDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(fileDir))
                    Directory.CreateDirectory(fileDir);
                await File.WriteAllTextAsync(filePath, result, cancellationToken);
            }

            return deserializedResult;
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(filePath))
                FileSystemHelper.DeleteFile(filePath);
            throw;
        }
    }

    /// <summary>
    ///     Downloads a file from the specified URL and saves it to the given file path.
    ///     If the file already exists at the specified path, it reads the content directly from the file.
    /// </summary>
    /// <param name="filePath">The path where the file will be saved.</param>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="sha1">
    ///     The expected SHA1 hash of the file for integrity verification. If null or empty, the hash check is
    ///     skipped.
    /// </param>
    /// <param name="progressReporter">An optional progress reporter for reporting download progress.</param>
    /// <param name="cancellationToken">A token to allow cancellation of the operation.</param>
    /// <returns>The content of the downloaded file, or null if the download fails.</returns>
    private async Task<string?> DownloadAndSaveFileAsync(string filePath, string url, string sha1,
        IProgressReporter? progressReporter, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(filePath) && (string.IsNullOrEmpty(sha1) || FileSystemHelper.CheckSHA1(filePath, sha1)))
            {
                progressReporter?.Report(0, Path.GetFileName(filePath), false);
                return await File.ReadAllTextAsync(filePath, cancellationToken);
            }

            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                progressReporter?.Report(e, Path.GetFileName(filePath));
            };

            var result = await _httpService.GetStringAsync(url, progress, cancellationToken);
            if (result == null)
                return null;
            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(fileDir))
                Directory.CreateDirectory(fileDir);
            await File.WriteAllTextAsync(filePath, result, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(filePath))
                FileSystemHelper.DeleteFile(filePath);
            throw;
        }
    }

    /// <summary>
    ///     Downloads a binary file from a URL and saves it locally if it doesn't already exist.
    /// </summary>
    /// <param name="filePath">The local file path where the file will be saved.</param>
    /// <param name="url">The URL from which the file will be downloaded.</param>
    /// <param name="sha1">
    ///     The expected SHA1 hash of the file for integrity verification. If null or empty, the hash check is
    ///     skipped.
    /// </param>
    /// <param name="progressReporter">An optional progress reporter for tracking download progress.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A byte array containing the file content or null if the operation fails.</returns>
    private async Task DownloadAndSaveBinaryFileAsync(string filePath, string url, string sha1,
        IProgressReporter? progressReporter, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(filePath) && (string.IsNullOrEmpty(sha1) || FileSystemHelper.CheckSHA1(filePath, sha1)))
            {
                progressReporter?.Report(0, Path.GetFileName(filePath), false);
                return;
            }

            var progress = new Progress<double>();
            progress.ProgressChanged += (_, e) =>
            {
                progressReporter?.Report(e, Path.GetFileName(filePath));
            };

            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(fileDir))
                Directory.CreateDirectory(fileDir);
            await _httpService.DownloadFileAsync(url, filePath, progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(filePath))
                FileSystemHelper.DeleteFile(filePath);
            throw;
        }
    }

    /// <summary>
    ///     Extracts native library files from a compressed archive and moves them to the specified directory.
    /// </summary>
    /// <param name="libFilePath">The file path of the compressed native library archive.</param>
    /// <param name="nativeDir">The directory where the extracted native files will be stored.</param>
    private void ExtractNativeFiles(string libFilePath, string nativeDir)
    {
        var tempDir = Path.Combine(nativeDir, Path.GetRandomFileName());
        try
        {
            ZipFile.ExtractToDirectory(libFilePath, tempDir, true);

            var searchPattern = "*.so";
            if (OSHelper.GetOperatingSystem() == EOperatingSystem.WINDOWS)
                searchPattern = "*.dll";

            foreach (var file in Directory.GetFiles(tempDir, searchPattern, SearchOption.AllDirectories))
            {
                if ((Environment.Is64BitOperatingSystem && file.Contains("32")) ||
                    (!Environment.Is64BitOperatingSystem && !file.Contains("32")))
                    continue;

                var destFile = Path.Combine(nativeDir, Path.GetFileName(file));
                if (!File.Exists(destFile))
                    File.Move(file, destFile, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to extract native files from {libFilePath}:");
        }
        finally
        {
            FileSystemHelper.DeleteDirectory(tempDir);
        }
    }
}
