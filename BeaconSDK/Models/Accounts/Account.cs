using System.ComponentModel;
using System.Text.Json.Serialization;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Models.MojangApi.User;
using Tavstal.BeaconSDK.Services.Abstractions;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Tavstal.BeaconSDK.Models.Accounts;

/// <summary>
///     Represents an account with properties such as ID, UUID, display name, account type,
///     access token, refresh token, expiration date, skins, and Mojang profile.
/// </summary>
[Serializable]
public class Account
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Account" /> class.
    /// </summary>
    public Account()
    {
        Id = string.Empty;
        Uuid = string.Empty;
        DisplayName = string.Empty;
        EncryptedAccessToken = [];
        EncryptedRefreshToken = [];
    }

    /// <summary>
    ///     Gets or sets the unique identifier of the account.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the UUID of the account.
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }

    /// <summary>
    ///     Gets or sets the display name of the account.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    /// <summary>
    ///     Gets or sets the type of the account.
    /// </summary>
    [JsonPropertyName("type")]
    public EAccountType Type { get; set; }

    /// <summary>
    ///     Gets or sets the encrypted access token.
    ///     This property is obsolete and should not be used directly.
    /// </summary>
    [JsonPropertyName("accessToken")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use GetAccessToken() instead.")]
    public byte[] EncryptedAccessToken { get; set; }

    /// <summary>
    ///     Gets or sets the encrypted refresh token.
    ///     This property is obsolete and should not be used directly.
    /// </summary>
    [JsonPropertyName("refreshToken")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use GetRefreshToken() instead.")]
    public byte[] EncryptedRefreshToken { get; set; }

    /// <summary>
    ///     Gets or sets the expiration date of the access token.
    /// </summary>
    [JsonPropertyName("accessTokenExpDate")]
    public DateTime AccessTokenExpireDate { get; set; }

    /// <summary>
    ///     Gets or sets the list of skins associated with the account.
    /// </summary>
    [JsonPropertyName("skins")]
    public List<AccountSkin> Skins { get; set; } = new();

    /// <summary>
    ///     Gets or sets the Mojang profile associated with the account.
    /// </summary>
    [JsonPropertyName("mojangProfile")]
    public MojangProfile? MojangProfile { get; set; }

    /// <summary>
    ///     Gets a value indicating whether the account can expire.
    /// </summary>
    public bool CanExpire => Type != EAccountType.OFFLINE;

    /// <summary>
    ///     Retrieves the decrypted access token. If the token is cached, it returns the cached value.
    /// </summary>
    /// <param name="encryptionService">The encryption service used to decrypt the stored token.</param>
    /// <returns>The decrypted access token.</returns>
    public byte[] GetAccessToken(IEncryptionService encryptionService)
    {
        return encryptionService.Decrypt(EncryptedAccessToken);
    }

    /// <summary>
    ///     Sets the access token and encrypts it for storage.
    /// </summary>
    /// <param name="plainAccessToken">The access token to set. Plaintext and already encrypted values are both accepted.</param>
    /// <param name="encryptionService">The encryption service used to encrypt the token for storage.</param>
    public void SetAccessToken(byte[] plainAccessToken, IEncryptionService encryptionService)
    {
        EncryptedAccessToken = encryptionService.Reprotect(plainAccessToken);
    }

    /// <summary>
    ///     Retrieves the decrypted refresh token. If the token is cached, it returns the cached value.
    /// </summary>
    /// <param name="encryptionService">The encryption service used to decrypt the stored token.</param>
    /// <returns>The decrypted refresh token.</returns>
    public byte[] GetRefreshToken(IEncryptionService encryptionService)
    {
        return encryptionService.Decrypt(EncryptedRefreshToken);
    }

    /// <summary>
    ///     Sets the refresh token and encrypts it for storage.
    /// </summary>
    /// <param name="plainRefreshToken">The refresh token to set. Plaintext and already encrypted values are both accepted.</param>
    /// <param name="encryptionService">The encryption service used to encrypt the token for storage.</param>
    public void SetRefreshToken(byte[] plainRefreshToken, IEncryptionService encryptionService)
    {
        EncryptedRefreshToken = encryptionService.Reprotect(plainRefreshToken);
    }
}
