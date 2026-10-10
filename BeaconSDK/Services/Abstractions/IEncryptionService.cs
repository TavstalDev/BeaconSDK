using System.Security.Cryptography;

namespace Tavstal.BeaconSDK.Services.Abstractions;

/// <summary>
///     Provides encryption and decryption of sensitive values such as authentication tokens.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    ///     Encrypts the given bytes using the encryption scheme available in the current environment.
    /// </summary>
    /// <param name="plainBytes">The bytes to encrypt.</param>
    /// <returns>The encrypted bytes, prefixed with a marker identifying the encryption scheme used.</returns>
    public byte[] Encrypt(byte[] plainBytes);

    /// <summary>
    ///     Decrypts the given bytes based on the encryption scheme identified by their prefix.
    /// </summary>
    /// <param name="cipherBytes">The bytes to decrypt.</param>
    /// <param name="ignoreUnknown">Whether to return the input unchanged when its format is not recognized.</param>
    /// <returns>The decrypted bytes.</returns>
    /// <exception cref="CryptographicException">
    ///     Thrown when the prefix is unrecognized and <paramref name="ignoreUnknown" /> is <see langword="false" />, or when
    ///     the data required to decrypt is not available (e.g. data protection is not configured).
    /// </exception>
    public byte[] Decrypt(byte[] cipherBytes, bool ignoreUnknown = false);

    /// <summary>
    ///     Reprotects encrypted bytes by decrypting them and re-encrypting them with the encryption scheme available in the
    ///     current environment.
    /// </summary>
    /// <param name="cipherBytes">The encrypted bytes to reprotect.</param>
    /// <returns>The reprotected bytes, or the original input if reprotection fails.</returns>
    public byte[] Reprotect(byte[] cipherBytes);
}
