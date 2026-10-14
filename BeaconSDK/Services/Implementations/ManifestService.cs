using System.Text.Json;
using Tavstal.BeaconSDK.Helpers.Serialization;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.ModLoaders;
using Tavstal.BeaconSDK.Models.ModLoaders.Fabric;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge;
using Tavstal.BeaconSDK.Models.ModLoaders.NeoForge;
using Tavstal.BeaconSDK.Models.ModLoaders.Quilt;
using Tavstal.BeaconSDK.Models.MojangApi;
using Tavstal.BeaconSDK.Services.Abstractions;
using CoreJsonContext = Tavstal.BeaconSDK.Models.Json.CoreJsonContext;

namespace Tavstal.BeaconSDK.Services.Implementations;

/// <inheritdoc cref="IManifestService" />
public class ManifestService : IManifestService
{
    // ReSharper disable once NotAccessedField.Local
    private readonly IBeaconLogger _logger;
    private readonly IHttpService _httpService;
    private VersionManifest? _vanillaManifest;
    private readonly BeaconConfiguration _config;
    private List<IModManifest>? _fabricManifest;
    private List<IModManifest>? _forgeManifest;
    private List<IModManifest>? _neoForgeManifest;
    private List<IModManifest>? _quiltManifest;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ManifestService" /> class.
    /// </summary>
    /// <param name="logger">Logger instance used to record diagnostic and error messages during manifest operations.</param>
    /// <param name="httpService">HTTP service used for remote downloads.</param>
    /// <param name="config">The Beacon SDK configuration providing the manifest endpoint URLs.</param>
    public ManifestService(IBeaconLogger<ManifestService> logger, IHttpService httpService, BeaconConfiguration config)
    {
        _logger = logger;
        _httpService = httpService;
        _config = config;
    }

    /// <inheritdoc />
    public VersionManifest? GetMinecraftManifest()
    {
        return _vanillaManifest;
    }

    /// <inheritdoc />
    public async Task<VersionManifest?> GetMinecraftManifestAsync(string manifestPath,
        CancellationToken cancellationToken = default)
    {
        if (_vanillaManifest != null)
            return _vanillaManifest;

        _vanillaManifest = await JsonHelper.ReadJsonFileAsync<VersionManifest>(manifestPath,
            CoreJsonContext.Default.VersionManifest, cancellationToken);
        return _vanillaManifest;
    }

    /// <inheritdoc />
    public async Task<VersionManifest?> DownloadMinecraftManifestAsync(string manifestPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = await _httpService.DownloadFileAsync(_config.Endpoints.Mojang.MinecraftManifestUrl, manifestPath, null, progress, cancellationToken);
        if (!result)
        {
            _logger.LogError("Failed to download vanilla manifest.");
            return null;
        }
        // Invalidate cache
        _vanillaManifest = null;
        return await GetMinecraftManifestAsync(manifestPath, cancellationToken);
    }

    /// <inheritdoc />
    public List<IModManifest>? GetFabricManifest()
    {
        return _fabricManifest;
    }

    /// <inheritdoc />
    public async Task<List<IModManifest>?> GetFabricManifestAsync(string manifestPath,
        CancellationToken cancellationToken = default)
    {
        if (_fabricManifest != null)
            return _fabricManifest;

        var rawManifest = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        var jObject = JsonElement.Parse(rawManifest);
        if (!jObject.TryGetProperty("loader", out var mappings) || mappings.GetArrayLength() == 0)
            throw new InvalidOperationException("Fabric manifest loader not found in the JSON.");
        _fabricManifest = [];
        foreach (var mapping in mappings.EnumerateArray())
        {
            if (!mapping.TryGetProperty("version", out var version))
                continue;

            _fabricManifest.Add(new FabricManifest(version.ToString()));
        }

        return _fabricManifest;
    }

    /// <inheritdoc />
    public async Task<List<IModManifest>?> DownloadFabricManifestAsync(string manifestPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = await _httpService.DownloadFileAsync(_config.Endpoints.Fabric.VersionManifestUrl, manifestPath, null, progress, cancellationToken);
        if (!result)
        {
            _logger.LogError("Failed to download fabric manifest.");
            return null;
        }
        // Invalidate cache
        _fabricManifest = null;
        return await GetFabricManifestAsync(manifestPath, cancellationToken);
    }

    /// <inheritdoc />
    public List<IModManifest>? GetQuiltManifest()
    {
        return _quiltManifest;
    }

    /// <inheritdoc />
    public async Task<List<IModManifest>?> GetQuiltManifestAsync(string manifestPath,
        CancellationToken cancellationToken = default)
    {
        if (_quiltManifest != null)
            return _quiltManifest;

        var rawManifest = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        var jObject = JsonElement.Parse(rawManifest);
        if (!jObject.TryGetProperty("loader", out var mappings) || mappings.GetArrayLength() == 0)
            throw new InvalidOperationException("Quilt manifest loader not found in the JSON.");
        _quiltManifest = [];
        foreach (var mapping in mappings.EnumerateArray())
        {
            if (!mapping.TryGetProperty("version", out var version))
                continue;
            _quiltManifest.Add(new QuiltManifest(version.ToString()));
        }

        return _quiltManifest;
    }

    /// <inheritdoc />
    public async Task<List<IModManifest>?> DownloadQuiltManifestAsync(string manifestPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = await _httpService.DownloadFileAsync(_config.Endpoints.Quilt.VersionManifestUrl, manifestPath, null, progress, cancellationToken);
        if (!result)
        {
            _logger.LogError("Failed to download quilt manifest.");
            return null;
        }
        // Invalidate cache
        _quiltManifest = null;
        return await GetQuiltManifestAsync(manifestPath, cancellationToken);
    }

    /// <inheritdoc />
    public List<IModManifest>? GetForgeManifest()
    {
        return _forgeManifest;
    }

    /// <inheritdoc />
    public async Task<List<IModManifest>?> GetForgeManifestAsync(string manifestPath,
        CancellationToken cancellationToken = default)
    {
        if (_forgeManifest != null)
            return _forgeManifest;

        _forgeManifest = [];
        var localManifests = await JsonHelper.ReadJsonFileAsync<List<ForgeManifest>>(manifestPath,
            CoreJsonContext.Default.ListForgeManifest, cancellationToken);
        if (localManifests == null)
            throw new InvalidOperationException("Forge manifest loader not found in the JSON.");

        foreach (var manifest in localManifests)
            _forgeManifest.Add(manifest);

        return _forgeManifest;
    }

    /// <inheritdoc />
    public async Task<List<IModManifest>?> DownloadForgeManifestAsync(string manifestPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = await _httpService.DownloadFileAsync(_config.Endpoints.Forge.VersionManifest, manifestPath, null, progress, cancellationToken);
        if (!result)
        {
            _logger.LogError("Failed to download forge manifest.");
            return null;
        }
        // Invalidate cache
        _forgeManifest = null;
        return await GetForgeManifestAsync(manifestPath, cancellationToken);
    }

    /// <inheritdoc />
    public List<IModManifest>? GetNeoForgeManifest()
    {
        return _neoForgeManifest;
    }

    /// <inheritdoc />
    public async Task<List<IModManifest>?> GetNeoForgeManifestAsync(string manifestPath, CancellationToken cancellationToken = default)
    {
        if (_neoForgeManifest != null)
            return _neoForgeManifest;

        _neoForgeManifest = [];
        var localManifests =
            await JsonHelper.ReadJsonFileAsync<List<NeoForgeManifest>>(manifestPath,
                CoreJsonContext.Default.ListNeoForgeManifest, cancellationToken);
        if (localManifests == null)
            throw new InvalidOperationException("Neo forge manifest loader not found in the JSON.");

        foreach (var manifest in localManifests)
            _neoForgeManifest.Add(manifest);

        return _neoForgeManifest;
    }


    /// <inheritdoc />
    public async Task<List<IModManifest>?> DownloadNeoForgeManifestAsync(string manifestPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = await _httpService.DownloadFileAsync(_config.Endpoints.NeoForge.VersionManifest, manifestPath, null, progress, cancellationToken);
        if (!result)
        {
            _logger.LogError("Failed to download neoforge manifest.");
            return null;
        }
        // Invalidate cache
        _neoForgeManifest = null;
        return await GetNeoForgeManifestAsync(manifestPath, cancellationToken);
    }
}
