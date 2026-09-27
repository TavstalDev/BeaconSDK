using Microsoft.AspNetCore.DataProtection;

namespace Tavstal.BeaconSDK.Services.Abstractions;

/// <summary>
/// Provides encryption and decryption of sensitive values such as authentication tokens.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// A data protector instance used for encrypting and decrypting data.
    /// </summary>
    public IDataProtector? DataProtector { get; protected set; }

    /// <summary>
    /// Encrypts the given text based on the current operating system.
    /// </summary>
    /// <param name="text">The text to encrypt.</param>
    /// <returns>The encrypted text.</returns>
    public string Encrypt(string text);

    /// <summary>
    /// Decrypts the given text based on the current operating system.
    /// </summary>
    /// <param name="text">The text to decrypt.</param>
    /// <param name="ignoreUnknown">Whether to return the input unchanged when its format is not recognized.</param>
    /// <returns>The decrypted text.</returns>
    public string Decrypt(string text, bool ignoreUnknown = false);

    /// <summary>
    /// Reprotects an encrypted string by decrypting it and re-encrypting it using the current data protection provider.
    /// </summary>
    /// <param name="encrypted">The encrypted string to be reprotected.</param>
    /// <returns>
    /// The reprotected string if the data protection provider is set; otherwise, the original encrypted string.
    /// </returns>
    /// <remarks>
    /// This method handles different encryption formats based on their prefixes:
    /// - If the prefix is "dpv1:", the string is decrypted and re-encrypted using the current data protection provider.
    /// - If the prefix is "winv1:", "linuxv1:", or "macv1:", the string is decrypted using the respective platform-specific method
    ///   and then re-encrypted using the current data protection provider.
    /// - If the format is unknown, the string is re-encrypted as-is.
    /// If the data protection provider is not set, the method returns the original encrypted string.
    /// </remarks>
    public string Reprotect(string encrypted);
}