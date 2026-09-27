using System.ComponentModel;
using System.Text.Json.Serialization;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Models.MojangApi.User;
using Tavstal.BeaconSDK.Services.Abstractions;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Tavstal.BeaconSDK.Models.Accounts;

/// <summary>
/// Represents an account with properties such as ID, UUID, display name, account type, 
/// access token, refresh token, expiration date, skins, and Mojang profile.
/// </summary>
[Serializable]
public class Account
{
    /// <summary>
    /// Gets or sets the unique identifier of the account.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the UUID of the account.
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }

    /// <summary>
    /// Gets or sets the display name of the account.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the type of the account.
    /// </summary>
    [JsonPropertyName("type")]
    public EAccountType Type { get; set; }

    /// <summary>
    /// Gets or sets the encrypted access token. 
    /// This property is obsolete and should not be used directly.
    /// </summary>
    [JsonPropertyName("accessToken")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use GetAccessToken() instead.")]
    public string EncryptedAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the encrypted refresh token. 
    /// This property is obsolete and should not be used directly.
    /// </summary>
    [JsonPropertyName("refreshToken")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use GetRefreshToken() instead.")]
    public string EncryptedRefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the access token.
    /// </summary>
    [JsonPropertyName("accessTokenExpDate")]
    public DateTime AccessTokenExpireDate { get; set; }

    /// <summary>
    /// Gets or sets the list of skins associated with the account.
    /// </summary>
    [JsonPropertyName("skins")]
    public List<AccountSkin> Skins { get; set; } = new();

    /// <summary>
    /// Gets or sets the Mojang profile associated with the account.
    /// </summary>
    [JsonPropertyName("mojangProfile")]
    public MojangProfile? MojangProfile { get; set; }
    
    /// <summary>
    /// Gets a value indicating whether the account can expire.
    /// </summary>
    public bool CanExpire => Type != EAccountType.OFFLINE;

    /// <summary>
    /// Initializes a new instance of the <see cref="Account"/> class.
    /// </summary>
    public Account() {}
    
    private string _accessTokenCache = string.Empty;

    /// <summary>
    /// Retrieves the decrypted access token. If the token is cached, it returns the cached value.
    /// </summary>
    /// <param name="encryptionService">The encryption service used to decrypt the stored token.</param>
    /// <returns>The decrypted access token.</returns>
    public string GetAccessToken(IEncryptionService encryptionService)
    {
        if (!string.IsNullOrEmpty(_accessTokenCache))
            return _accessTokenCache;
        if (encryptionService.DataProtector != null)
            return EncryptedAccessToken;
        _accessTokenCache = encryptionService.Decrypt(EncryptedAccessToken);
        return _accessTokenCache;
    }

    /// <summary>
    /// Sets the access token and encrypts it for storage.
    /// </summary>
    /// <param name="accessToken">The access token to set.</param>
    /// <param name="encryptionService">The encryption service used to encrypt the token for storage.</param>
    public void SetAccessToken(string accessToken, IEncryptionService encryptionService)
    {
        _accessTokenCache = accessToken;
        EncryptedAccessToken = encryptionService.Reprotect(accessToken);
    }

    private string _refreshTokenCache = string.Empty;

    /// <summary>
    /// Retrieves the decrypted refresh token. If the token is cached, it returns the cached value.
    /// </summary>
    /// <param name="encryptionService">The encryption service used to decrypt the stored token.</param>
    /// <returns>The decrypted refresh token.</returns>
    public string GetRefreshToken(IEncryptionService encryptionService)
    {
        if (!string.IsNullOrEmpty(_refreshTokenCache))
            return _refreshTokenCache;
        if (encryptionService.DataProtector != null)
            return EncryptedRefreshToken;
        _refreshTokenCache = encryptionService.Decrypt(EncryptedRefreshToken);
        return _refreshTokenCache;
    }

    /// <summary>
    /// Sets the refresh token and encrypts it for storage.
    /// </summary>
    /// <param name="refreshToken">The refresh token to set.</param>
    /// <param name="encryptionService">The encryption service used to encrypt the token for storage.</param>
    public void SetRefreshToken(string refreshToken, IEncryptionService encryptionService)
    {
        _refreshTokenCache = refreshToken;
        EncryptedRefreshToken = encryptionService.Reprotect(refreshToken);
    }
}
