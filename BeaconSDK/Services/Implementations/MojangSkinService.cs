using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tavstal.BeaconSDK.Models.Endpoints;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.MojangApi.Requests;
using Tavstal.BeaconSDK.Models.MojangApi.User;
using Tavstal.BeaconSDK.Services.Abstractions;
using CoreJsonContext = Tavstal.BeaconSDK.Models.Json.CoreJsonContext;

namespace Tavstal.BeaconSDK.Services.Implementations;

/// <inheritdoc />
public class MojangSkinService : IMojangSkinService
{
    private readonly IHttpService _httpService;
    private readonly ICustomLogger _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MojangSkinService" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The logger instance used for recording diagnostic information, warnings, and errors related to
    ///     Mojang skin and cape API operations.
    /// </param>
    /// <param name="httpService">
    ///     The HTTP service instance used for making authenticated requests to the Microsoft/Mojang
    ///     player configuration API endpoints.
    /// </param>
    public MojangSkinService(ICustomLogger<MojangSkinService> logger, IHttpService httpService)
    {
        _logger = logger;
        _httpService = httpService;
    }

    /// <inheritdoc />
    public async Task<MojangProfile?> ChangeSkinAsync(string mcToken, string variant, string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string endpoint = $"{MicrosoftEndpoints.PlayerConfigUrl}/skins";
            object body = new ChangeSkinRequestBody { Variant = variant, Url = url };

            var reqContent = new StringContent(
                JsonSerializer.Serialize(body, CoreJsonContext.Default.ChangeSkinRequestBody),
                Encoding.UTF8,
                "application/json"
            );

            var client = _httpService.CreateHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);
            var result = await client.PostAsync(endpoint, reqContent, cancellationToken);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to change skin (HTTP {result.StatusCode}): " +
                                 await result.Content.ReadAsStringAsync(cancellationToken));
                return null;
            }

            return JsonSerializer.Deserialize<MojangProfile>(await result.Content.ReadAsStringAsync(cancellationToken),
                CoreJsonContext.Default.MojangProfile);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to change skin: " + ex.Message);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<MojangProfile?> UploadSkinAsync(string mcToken, string variant, string skinPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string endpoint = $"{MicrosoftEndpoints.PlayerConfigUrl}/skins";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(variant), "variant");
            await using var fs = File.OpenRead(skinPath);
            var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "file", Path.GetFileName(skinPath));

            var client = _httpService.CreateHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);
            var result = await client.PostAsync(endpoint, form, cancellationToken);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to upload skin (HTTP {result.StatusCode}): " +
                                 await result.Content.ReadAsStringAsync(cancellationToken));
                return null;
            }

            return JsonSerializer.Deserialize<MojangProfile>(await result.Content.ReadAsStringAsync(cancellationToken),
                CoreJsonContext.Default.MojangProfile);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to upload skin: " + ex.Message);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<MojangProfile?> ResetSkinAsync(string mcToken, long playerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = $"{MicrosoftEndpoints.PlayerConfigUrl}/skins/active?uuid={playerId}";
            var client = _httpService.CreateHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);
            var result = await client.DeleteAsync(endpoint, cancellationToken);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to reset skin (HTTP {result.StatusCode}): " +
                                 await result.Content.ReadAsStringAsync(cancellationToken));
                return null;
            }

            return JsonSerializer.Deserialize<MojangProfile>(await result.Content.ReadAsStringAsync(cancellationToken),
                CoreJsonContext.Default.MojangProfile);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to reset skin: " + ex.Message);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<MojangProfile?> ShowCapeAsync(string mcToken, string capeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const string endpoint = $"{MicrosoftEndpoints.PlayerConfigUrl}/capes/active";
            object body = new ShowCapeRequestBody { CapeId = capeId };

            var reqContent = new StringContent(
                JsonSerializer.Serialize(body, CoreJsonContext.Default.ShowCapeRequestBody),
                Encoding.UTF8,
                "application/json"
            );
            var client = _httpService.CreateHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);
            var result = await client.PutAsync(endpoint, reqContent, cancellationToken);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to show cape (HTTP {result.StatusCode}): " +
                                 await result.Content.ReadAsStringAsync(cancellationToken));
                return null;
            }

            return JsonSerializer.Deserialize<MojangProfile>(await result.Content.ReadAsStringAsync(cancellationToken),
                CoreJsonContext.Default.MojangProfile);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to show cape: " + ex.Message);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<MojangProfile?> HideCapeAsync(string mcToken, CancellationToken cancellationToken = default)
    {
        try
        {
            const string endpoint = $"{MicrosoftEndpoints.PlayerConfigUrl}/capes/active";
            var client = _httpService.CreateHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);
            var result = await client.DeleteAsync(endpoint, cancellationToken);
            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to hide cape (HTTP {result.StatusCode}): " +
                                 await result.Content.ReadAsStringAsync(cancellationToken));
                return null;
            }

            return JsonSerializer.Deserialize<MojangProfile>(await result.Content.ReadAsStringAsync(cancellationToken),
                CoreJsonContext.Default.MojangProfile);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to hide cape: " + ex.Message);
            return null;
        }
    }
}
