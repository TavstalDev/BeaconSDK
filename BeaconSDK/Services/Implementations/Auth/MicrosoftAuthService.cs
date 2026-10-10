using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Accounts;
using Tavstal.BeaconSDK.Models.Endpoints;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.Microsoft;
using Tavstal.BeaconSDK.Models.MojangApi.User;
using Tavstal.BeaconSDK.Services.Abstractions;
using Tavstal.BeaconSDK.Services.Abstractions.Auth;
using CoreJsonContext = Tavstal.BeaconSDK.Models.Json.CoreJsonContext;

namespace Tavstal.BeaconSDK.Services.Implementations.Auth;

/// <inheritdoc />
public class MicrosoftAuthService : IMicrosoftAuthService
{
    private readonly IEncryptionService _encryptionService;
    private readonly IHttpService _httpService;
    private readonly IBeaconLogger _logger;

    private readonly string _redirectAuthenticateUrl =
        Path.Combine(MicrosoftHttpAuthService.ListeningUrl, "microsoft/authcallback");

    private Account? _account;

    private EAuthStatus _authStatus = EAuthStatus.NONE;
    private string _microsoftClientId = "496a0c42-aa74-41fe-b7bc-0ad155cdaa26";

    private MojangProfile? _mojangProfile;
    private IProgressReporter? _progressReporter;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MicrosoftAuthService" /> class with the
    ///     logger and HTTP service used to perform Microsoft authentication requests.
    /// </summary>
    /// <param name="logger">Logger used for diagnostics and error reporting.</param>
    /// <param name="httpService">HTTP service used to create configured <see cref="HttpClient" /> instances.</param>
    /// <param name="encryptionService">Encryption service used to store and reprotect account tokens.</param>
    public MicrosoftAuthService(IBeaconLogger<MicrosoftAuthService> logger, IHttpService httpService,
        IEncryptionService encryptionService)
    {
        _logger = logger;
        _httpService = httpService;
        _encryptionService = encryptionService;
    }

    /// <inheritdoc />
    public EAuthStatus AuthStatus => _authStatus;

    /// <inheritdoc />
    public MojangProfile? MojangProfile => _mojangProfile;

    /// <inheritdoc />
    public Account? Account => _account;

    /// <inheritdoc />
    public event IMicrosoftAuthService.AuthStatusChangedHandler? OnAuthStatusChanged;

    /// <inheritdoc />
    public void SetClientId(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogError("Microsoft client ID cannot be null or empty.");
            return;
        }

        _microsoftClientId = clientId;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _progressReporter = null;
        _account = null;
        _mojangProfile = null;
        _authStatus = EAuthStatus.NONE;
        OnAuthStatusChanged?.Invoke(_authStatus);
    }

    /// <inheritdoc />
    public void OpenAuthenticationUrl()
    {
        if (string.IsNullOrEmpty(_microsoftClientId))
        {
            _logger.LogError("Microsoft client ID is not set.");
            return;
        }

        var authUrl = MicrosoftEndpoints.MakeMicrosoftAuthUrl(_microsoftClientId, _redirectAuthenticateUrl);
        OSHelper.OpenUrl(authUrl);
    }

    /// <inheritdoc />
    public async Task HandleHttpRequestAsync(HttpListenerRequest request, IProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        _authStatus = EAuthStatus.PENDING;
        OnAuthStatusChanged?.Invoke(_authStatus);
        _progressReporter = progressReporter;
        if (string.IsNullOrEmpty(_microsoftClientId))
        {
            _logger.LogError("Microsoft client ID is not set.");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
            return;
        }

        if (!request.QueryString.AllKeys.Contains("code"))
        {
            _logger.LogError("HTTP request does not contain 'code' query parameter.");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
            return;
        }

        var code = request.QueryString["code"];
        if (string.IsNullOrEmpty(code))
        {
            _logger.LogError("Received 'code' query parameter is null or empty.");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
            return;
        }

        const string requestUrl = MicrosoftEndpoints.MicrosoftTokenUrl;
        _progressReporter?.Report(EProgressKind.AUTH_MICROSOFT_AUTHENTICATING, 0);

        try
        {
            var requestParams = new Dictionary<string, string>
            {
                { "client_id", _microsoftClientId },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", _redirectAuthenticateUrl }
            };
            var requestContent = new FormUrlEncodedContent(requestParams);

            using var client = _httpService.CreateHttpClient();
            var response = await client.PostAsync(requestUrl, requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get access token from Microsoft. Status code: " + response.StatusCode);
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var obj = JsonElement.Parse(responseString);
            if (!obj.TryGetProperty("access_token", out var value))
            {
                _logger.LogError("Access token not found in the Microsoft authentication response.\"");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            if (!obj.TryGetProperty("refresh_token", out var refreshToken))
            {
                _logger.LogError("Refresh token not found in the Microsoft authentication response.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            _authStatus = EAuthStatus.PROCESSING;
            OnAuthStatusChanged?.Invoke(_authStatus);

            // Proceed with the token
            await XboxTokenCallAsync(value.ToString(), refreshToken.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while handling HTTP request for Microsoft authentication:");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
        }
    }

    /// <inheritdoc />
    public async Task<DeviceCodeResult?> CreateDeviceCodeAsync(IProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _progressReporter?.Report(EProgressKind.AUTH_CODE_CREATING, 0);

            var parameters = new Dictionary<string, string>
            {
                { "client_id", _microsoftClientId }, { "scope", "XboxLive.signin offline_access" }
            };

            var formContent = new FormUrlEncodedContent(parameters);

            using var client = _httpService.CreateHttpClient();
            var result = await client.PostAsync(MicrosoftEndpoints.MicrosoftDeviceUrl, formContent, cancellationToken);

            var rawJson = await result.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<DeviceCodeResult>(rawJson, CoreJsonContext.Default.DeviceCodeResult);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while create device code:");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task CheckDeviceCodeAsync(string deviceCode, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Device code checked at " + DateTime.Now.ToString("HH:mm:ss"));
            var parameters = new Dictionary<string, string>
            {
                { "client_id", _microsoftClientId },
                { "device_code", deviceCode },
                { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" }
            };

            var formContent = new FormUrlEncodedContent(parameters);

            using var client = _httpService.CreateHttpClient();
            var response = await client.PostAsync(MicrosoftEndpoints.MicrosoftDeviceTokenUrl, formContent,
                cancellationToken);

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var obj = JsonElement.Parse(responseString);
            if (!obj.TryGetProperty("access_token", out var value))
                return;

            if (!obj.TryGetProperty("refresh_token", out var refreshToken))
                return;

            _authStatus = EAuthStatus.PROCESSING;
            OnAuthStatusChanged?.Invoke(_authStatus);

            await XboxTokenCallAsync(value.ToString(), refreshToken.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while create device code:");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
        }
    }

    /// <inheritdoc />
    public async Task XboxTokenCallAsync(string token, string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _progressReporter?.Report(EProgressKind.AUTH_XBOX_AUTHENTICATING, 0);

            var body = new XboxTokenRequestBody
            {
                Properties = new XboxTokenProperties
                {
                    AuthMethod = "RPS", SiteName = "user.auth.xboxlive.com", RpsTicket = $"d={token}"
                },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT"
            };

            var reqContent = new StringContent(
                JsonSerializer.Serialize(body, CoreJsonContext.Default.XboxTokenRequestBody),
                Encoding.UTF8,
                "application/json"
            );

            using var client = _httpService.CreateHttpClient();
            var result = await client.PostAsync(MicrosoftEndpoints.XboxAuthUrl, reqContent, cancellationToken);

            var resultObj = JsonElement.Parse(await result.Content.ReadAsStringAsync(cancellationToken));
            if (!resultObj.TryGetProperty("Token", out var value))
            {
                _logger.LogError("Token not found in the Xbox authentication response.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            await XboxXstsCallAsync(value.ToString(), refreshToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while making Xbox token call:");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
        }
    }

    /// <inheritdoc />
    public async Task XboxXstsCallAsync(string token, string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _progressReporter?.Report(EProgressKind.AUTH_XBOX_XSTS, 0);

            var body = new XboxXstsRequestBody
            {
                Properties = new XboxXstsProperties { SandboxId = "RETAIL", UserTokens = [token] },
                RelyingParty = "rp://api.minecraftservices.com/",
                TokenType = "JWT"
            };

            var reqContent = new StringContent(
                JsonSerializer.Serialize(body, CoreJsonContext.Default.XboxXstsRequestBody),
                Encoding.UTF8,
                "application/json"
            );

            using var client = _httpService.CreateHttpClient();
            var result = await client.PostAsync(MicrosoftEndpoints.XboxXstsUrl, reqContent, cancellationToken);

            var rawJson = await result.Content.ReadAsStringAsync(cancellationToken);
            var resultObj = JsonElement.Parse(rawJson);
            if (!resultObj.TryGetProperty("Token", out var value))
            {
                _logger.LogError("Token not found in the Xbox XSTS response.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            if (!resultObj.TryGetProperty("DisplayClaims", out var displayClaims))
            {
                _logger.LogError("DisplayClaims not found in the Xbox XSTS response.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            if (!displayClaims.TryGetProperty("xui", out var xui))
            {
                _logger.LogError("xui not found or empty in the Xbox XSTS response.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            JsonElement? firstXui = xui.GetArrayLength() > 0 ? xui[0] : null;
            if (firstXui == null)
            {
                _logger.LogError("User hash (uhs) not found in the Xbox XSTS response.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            if (!firstXui.Value.TryGetProperty("uhs", out var userHash))
            {
                _logger.LogError("User hash (uhs) is null in the Xbox XSTS response.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            await MinecraftAccessCallAsync(value.ToString(), refreshToken, userHash.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while making Xbox XSTS call:");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
        }
    }

    /// <inheritdoc />
    public async Task MinecraftAccessCallAsync(string token, string refreshToken, string userHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _progressReporter?.Report(EProgressKind.AUTH_MINECRAFT_AUTHENTICATING, 0);

            var body = new MinecraftAccessRequestBody
            {
                IdentityToken = $"XBL3.0 x={userHash};{token}", EnsureLegacyEnabled = true
            };

            var reqContent = new StringContent(
                JsonSerializer.Serialize(body, CoreJsonContext.Default.MinecraftAccessRequestBody),
                Encoding.UTF8,
                "application/json"
            );

            using var client = _httpService.CreateHttpClient();
            var result = await client.PostAsync(MicrosoftEndpoints.MinecraftAuthUrl, reqContent, cancellationToken);

            var resultObj = JsonElement.Parse(await result.Content.ReadAsStringAsync(cancellationToken));
            if (!resultObj.TryGetProperty("access_token", out var minecraftToken))
            {
                _logger.LogError("Access token not found in the Minecraft authentication response.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            if (!resultObj.TryGetProperty("expires_in", out var expiresIn))
            {
                _logger.LogError("Expiration time not found in the Minecraft authentication response.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            await CheckMinecraftOwnershipAsync(minecraftToken.ToString(), refreshToken, int.Parse(expiresIn.ToString()),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while making Minecraft access call:");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
        }
    }

    /// <inheritdoc />
    public async Task CheckMinecraftOwnershipAsync(string mcToken, string refreshToken, int expireSeconds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _progressReporter?.Report(EProgressKind.AUTH_MINECRAFT_OWNERSHIP, 0);

            using var client = _httpService.CreateHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);
            var result = await client.GetAsync(MicrosoftEndpoints.MinecraftOwnershipUrl, cancellationToken);

            var ownershipData = JsonSerializer.Deserialize(await result.Content.ReadAsStringAsync(cancellationToken),
                CoreJsonContext.Default.OwnershipData);

            var gameOwnership = ownershipData?.Items.Find(x =>
                x.Name is "game_minecraft" or "game_minecraft_bedrock" ||
                x.Name == "product_minecraft" ||
                x.Name == "product_minecraft_bedrock"
            );
            if (gameOwnership == null)
            {
                _logger.LogError("User does not own Minecraft.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            await GetMinecraftProfileAsync(mcToken, refreshToken, expireSeconds, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while checking Minecraft ownership:");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
        }
    }

    /// <inheritdoc />
    public async Task GetMinecraftProfileAsync(string mcToken, string refreshToken, int expireSecs,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _progressReporter?.Report(EProgressKind.AUTH_MINECRAFT_PROFILE, 0);

            using var client = _httpService.CreateHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);
            var result = await client.GetAsync(MicrosoftEndpoints.MinecraftProfileUrl, cancellationToken);

            _mojangProfile =
                JsonSerializer.Deserialize(await result.Content.ReadAsStringAsync(cancellationToken),
                    CoreJsonContext.Default.MojangProfile);

            if (_mojangProfile == null)
            {
                _logger.LogError("Failed to retrieve Minecraft profile.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return;
            }

            List<AccountSkin> skins = [];
            if (_mojangProfile.Skins.Count > 0)
            {
                var cape = _mojangProfile.Capes.Find(x => x.State.Equals("active", StringComparison.OrdinalIgnoreCase));
                foreach (var skin in _mojangProfile.Skins)
                    skins.Add(new AccountSkin(Guid.NewGuid().ToString(), skin.Variant, cape?.Id, skin.Id));
            }

            _account = new Account
            {
                Id = Guid.NewGuid().ToString(),
                Uuid = _mojangProfile.Id,
                DisplayName = _mojangProfile.Name,
                Type = EAccountType.MICROSOFT,
                AccessTokenExpireDate = DateTime.Now.AddSeconds(expireSecs),
                Skins = skins,
                MojangProfile = _mojangProfile
            };
            byte[] accessTokenBytes = Encoding.UTF8.GetBytes(mcToken);
            byte[] refreshTokenBytes = Encoding.UTF8.GetBytes(refreshToken);
            _account.SetAccessToken(accessTokenBytes, _encryptionService);
            _account.SetRefreshToken(refreshTokenBytes, _encryptionService);
            Array.Clear(accessTokenBytes);
            Array.Clear(refreshTokenBytes);

            _authStatus = EAuthStatus.SUCCESS;
            OnAuthStatusChanged?.Invoke(_authStatus);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while getting Minecraft profile:");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
        }
    }

    /// <inheritdoc />
    public async Task<MojangProfile?> FetchMinecraftProfileAsync(string mcToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _progressReporter?.Report(EProgressKind.AUTH_MINECRAFT_PROFILE, 0);

            using var client = _httpService.CreateHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);
            var result = await client.GetAsync(MicrosoftEndpoints.MinecraftProfileUrl, cancellationToken);

            return JsonSerializer.Deserialize(await result.Content.ReadAsStringAsync(cancellationToken),
                CoreJsonContext.Default.MojangProfile);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while getting Minecraft profile:");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RefreshLoginAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_microsoftClientId))
        {
            _logger.LogError("Microsoft client ID is not set.");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
            return false;
        }

        const string requestUrl = MicrosoftEndpoints.MicrosoftTokenUrl;

        try
        {
            var requestParams = new Dictionary<string, string>
            {
                { "client_id", _microsoftClientId }, { "grant_type", "refresh_token" }, { "refresh_token", token }
            };
            var requestContent = new FormUrlEncodedContent(requestParams);

            using var client = _httpService.CreateHttpClient();
            var response = await client.PostAsync(requestUrl, requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get access token from Microsoft. Status code: " + response.StatusCode);
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return false;
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

            var obj = JsonElement.Parse(responseString);
            if (!obj.TryGetProperty("access_token", out var value))
            {
                _logger.LogError("Access token not found in the Microsoft authentication response.\"");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return false;
            }

            if (!obj.TryGetProperty("refresh_token", out var refreshToken))
            {
                _logger.LogError("Refresh token not found in the Microsoft authentication response.");
                _authStatus = EAuthStatus.FAILED;
                OnAuthStatusChanged?.Invoke(_authStatus);
                return false;
            }

            // Proceed with the token
            await XboxTokenCallAsync(value.ToString(), refreshToken.ToString(), cancellationToken);
            // Wait for the authentication process to complete
            _logger.LogDebug("Refresh auth status: " + AuthStatus);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while handling HTTP request for Microsoft authentication:");
            _authStatus = EAuthStatus.FAILED;
            OnAuthStatusChanged?.Invoke(_authStatus);
            return false;
        }
    }
}
