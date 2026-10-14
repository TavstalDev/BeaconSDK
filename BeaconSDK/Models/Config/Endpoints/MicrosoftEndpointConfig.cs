using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config.Endpoints;

/// <summary>
///     Represents the Microsoft identity platform and Xbox Live endpoint URLs used during authentication.
/// </summary>
public class MicrosoftEndpointConfig
{
    /// <summary>
    ///     Gets or sets the OAuth 2.0 authorization endpoint used to start the interactive sign-in flow.
    /// </summary>
    [JsonPropertyName("AuthUrl")]
    public string AuthUrl { get; set; } = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";

    /// <summary>
    ///     Gets or sets the OAuth 2.0 token endpoint used to exchange authorization codes and refresh tokens.
    /// </summary>
    [JsonPropertyName("TokenUrl")]
    public string TokenUrl { get; set; } = "https://login.live.com/oauth20_token.srf";

    /// <summary>
    ///     Gets or sets the device code endpoint used to begin the device code authentication flow.
    /// </summary>
    [JsonPropertyName("DeviceUrl")]
    public string DeviceUrl { get; set; } = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";

    /// <summary>
    ///     Gets or sets the token endpoint used to poll for a device code authentication result.
    /// </summary>
    [JsonPropertyName("DeviceTokenUrl")]
    public string DeviceTokenUrl { get; set; } = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";

    /// <summary>
    ///     Gets or sets the Xbox Live authentication endpoint that exchanges a Microsoft access token for an Xbox token.
    /// </summary>
    [JsonPropertyName("XboxAuthUrl")]
    public string XboxAuthUrl { get; set; } = "https://user.auth.xboxlive.com/user/authenticate";

    /// <summary>
    ///     Gets or sets the Xbox Live XSTS authorization endpoint that exchanges an Xbox token for an XSTS token.
    /// </summary>
    [JsonPropertyName("XboxXstsUrl")]
    public string XboxXstsUrl { get; set; } = "https://xsts.auth.xboxlive.com/xsts/authorize";

    /// <summary>
    ///     Builds the full Microsoft authorization URL for the interactive sign-in flow.
    /// </summary>
    /// <param name="clientId">The Microsoft OAuth2 client ID used for the request.</param>
    /// <param name="redirectUrl">The redirect URI that receives the authorization code.</param>
    /// <returns>The complete authorization URL, including the query string.</returns>
    public string MakeMicrosoftAuthUrl(string clientId, string redirectUrl)
    {
        return $"{AuthUrl}?" +
               $"client_id={clientId}" +
               $"&response_type=code" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUrl)}" +
               $"&response_mode=query" +
               $"&scope=XboxLive.signin%20offline_access";
    }
}
