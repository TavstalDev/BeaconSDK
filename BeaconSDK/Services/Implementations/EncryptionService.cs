using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Services.Implementations;

/// <inheritdoc />
/// <remarks>
/// <para>
/// <b>SECURITY WARNING:</b> If an <see cref="IDataProtectionProvider" /> instance is not provided during initialization,
/// this service falls back to platform-specific encryption: DPAPI scoped to the current user on Windows, and AES with a
/// static embedded key on Linux and macOS.
/// </para>
/// <para>
/// This fallback mechanism is intended strictly for unconfigured or lower-tier environments. For production workloads,
/// ensure <see cref="IDataProtectionProvider" /> is registered via Dependency Injection to guarantee key derivation and
/// rotation.
/// </para>
/// </remarks>
public class EncryptionService : IEncryptionService
{
    private const int PREFIX_LENGTH = 6;
    // As of v1 all prefix lengths should be 6 characters including the ':'.
    private readonly byte[] PREFIX_V1_DP = [.. "dpsv1:"u8];
    private readonly byte[] PREFIX_V1_WIN = [.. "winv1:"u8];
    private readonly byte[] PREFIX_V1_LINUX = [.. "linv1:"u8];
    private readonly byte[] PREFIX_V1_MAC = [.. "macv1:"u8];
    private readonly byte[] E_KEY_LINUX = [.. "5aEcj3cLUm5qYeCpptEaQoFCUTgZyEZw"u8];
    private readonly byte[] E_KEY_MAC = [.. "MqcM2kVeroKpvh9z79t3KjtiTzCUKSRs"u8];
    private readonly byte[] S_ENTROPY = [.. "8AE1C425-241D-46A9-A8F8-6E8A68D2586C"u8];
    private readonly ICustomLogger<EncryptionService> _logger;
    private readonly IDataProtector? _dataProtector;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EncryptionService" /> class.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostic messages, warnings, and errors.</param>
    /// <param name="provider">
    ///     The data protection protector used to encrypt and decrypt values. When <see langword="null" />,
    ///     platform-specific encryption is used instead.
    /// </param>
    public EncryptionService(ICustomLogger<EncryptionService> logger, IDataProtectionProvider? provider = null)
    {
        _logger = logger;
        if (provider == null)
        {
            logger.LogWarning(
                "DataProtector is not configured. Using weak fallback encryption with a static key. " +
                "Configure DataProtection for production security."
            );
            return;
        }
        _dataProtector = provider.CreateProtector("BeaconSDK.Encryption.v1");
    }

    /// <inheritdoc />
    public byte[] Encrypt(byte[] plainBytes)
    {
        if (_dataProtector != null)
            return ApplyPrefix(PREFIX_V1_DP, _dataProtector.Protect(plainBytes));

        return OSHelper.GetOperatingSystem() switch
        {
            EOperatingSystem.WINDOWS => ApplyPrefix(PREFIX_V1_WIN, EncryptWin(plainBytes)),
            EOperatingSystem.LINUX => ApplyPrefix(PREFIX_V1_LINUX, EncryptLinux(plainBytes)),
            EOperatingSystem.MACOS => ApplyPrefix(PREFIX_V1_MAC, EncryptMac(plainBytes)),
            _ => throw new Exception("Unsupported operating system.")
        };
    }

    /// <inheritdoc />
    public byte[] Decrypt(byte[] cipherBytes, bool ignoreUnknown = false)
    {
        // If cypherBytes is equals or less than the smallest known prefix it is considered unencrypted.
        if (cipherBytes.Length <= PREFIX_LENGTH)
            return cipherBytes;

        byte[] prefixBytes = GetPrefix(cipherBytes);
        int encryptedDataLength = cipherBytes.Length - prefixBytes.Length;
        if (encryptedDataLength <= 0)
            return cipherBytes;

        byte[] encryptedBytes = new byte[encryptedDataLength];
        Buffer.BlockCopy(cipherBytes, prefixBytes.Length, encryptedBytes, 0, encryptedBytes.Length);
        try
        {
            // DPv1
            if (prefixBytes.SequenceEqual(PREFIX_V1_DP))
            {
                if (_dataProtector == null)
                    throw new CryptographicException("DataProtector is not configured, the cipherBytes cannot be decrypted.");
                return _dataProtector.Unprotect(encryptedBytes);
            }

            // Win v1
            if (prefixBytes.SequenceEqual(PREFIX_V1_WIN))
                return DecryptWin(encryptedBytes);

            // Linux v1
            if (prefixBytes.SequenceEqual(PREFIX_V1_LINUX))
                return DecryptLinux(encryptedBytes);

            // Mac v1
            if (prefixBytes.SequenceEqual(PREFIX_V1_MAC))
                return DecryptMac(encryptedBytes);

            // The prefix is unknown or not encrypted at all
            if (ignoreUnknown)
                return cipherBytes;
            throw new CryptographicException("The provided bytes has no known encryption format.");
        }
        finally
        {
            Array.Clear(prefixBytes);
            Array.Clear(encryptedBytes);
        }
    }

    /// <inheritdoc />
    public byte[] Reprotect(byte[] cipherBytes)
    {
        byte[] unencryptedBytes = [];
        try
        {
            unencryptedBytes = Decrypt(cipherBytes);
            return Encrypt(unencryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occured while reprotecting data.");
            return cipherBytes;
        }
        finally
        {
            Array.Clear(unencryptedBytes);
        }
    }

    private byte[] ApplyPrefix(byte[] prefixBytes, byte[] data)
    {
        byte[] newPrefixedBytes = new byte[prefixBytes.Length + data.Length];
        Buffer.BlockCopy(prefixBytes, 0, newPrefixedBytes, 0, prefixBytes.Length);
        Buffer.BlockCopy(data, 0, newPrefixedBytes, prefixBytes.Length, data.Length);
        return  newPrefixedBytes;
    }

    private byte[] GetPrefix(byte[] cipherBytes)
    {
        // As of v1 all prefixes has 6 bytes including the ':'.
        byte[] prefixBytes = new byte[PREFIX_LENGTH];
        Buffer.BlockCopy(cipherBytes, 0, prefixBytes, 0, PREFIX_LENGTH);
        return prefixBytes;
    }

    /// <summary>
    ///     Encrypts bytes using Windows DPAPI, scoped to the current user.
    /// </summary>
    /// <param name="plainBytes">The bytes to encrypt.</param>
    /// <returns>The encrypted bytes.</returns>
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private byte[] EncryptWin(byte[] plainBytes)
    {
        var encryptedBytes = ProtectedData.Protect(plainBytes, S_ENTROPY, DataProtectionScope.CurrentUser);
        return encryptedBytes;
    }

    /// <summary>
    ///     Decrypts bytes using Windows DPAPI, scoped to the current user.
    /// </summary>
    /// <param name="cipherBytes">The bytes to decrypt.</param>
    /// <returns>The decrypted bytes.</returns>
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private byte[] DecryptWin(byte[] cipherBytes)
    {
        var decryptedBytes = ProtectedData.Unprotect(cipherBytes, S_ENTROPY, DataProtectionScope.CurrentUser);
        return decryptedBytes;
    }

    /// <summary>
    ///     Encrypts bytes using AES with the Linux fallback key.
    /// </summary>
    /// <param name="plainBytes">The bytes to encrypt.</param>
    /// <returns>The encrypted bytes, or the input unchanged when it is empty.</returns>
    private byte[] EncryptLinux(byte[] plainBytes)
    {
        if (plainBytes.Length == 0)
            return plainBytes;
        return Encrypt(E_KEY_LINUX, plainBytes);
    }

    /// <summary>
    ///     Decrypts bytes using AES with the Linux fallback key.
    /// </summary>
    /// <param name="cipherBytes">The bytes to decrypt.</param>
    /// <returns>The decrypted bytes, or the input unchanged when it is empty.</returns>
    private byte[] DecryptLinux(byte[] cipherBytes)
    {
        if (cipherBytes.Length == 0)
            return cipherBytes;
        return Decrypt(E_KEY_LINUX, cipherBytes);
    }

    /// <summary>
    ///     Encrypts bytes using AES with the macOS fallback key.
    /// </summary>
    /// <param name="plainBytes">The bytes to encrypt.</param>
    /// <returns>The encrypted bytes, or the input unchanged when it is empty.</returns>
    private byte[] EncryptMac(byte[] plainBytes)
    {
        if (plainBytes.Length == 0)
            return plainBytes;
        return Encrypt(E_KEY_MAC, plainBytes);
    }

    /// <summary>
    ///     Decrypts bytes using AES with the macOS fallback key.
    /// </summary>
    /// <param name="cipherBytes">The bytes to decrypt.</param>
    /// <returns>The decrypted bytes, or the input unchanged when it is empty.</returns>
    private byte[] DecryptMac(byte[] cipherBytes)
    {
        if (cipherBytes.Length == 0)
            return cipherBytes;
        return Decrypt(E_KEY_MAC, cipherBytes);
    }

    /// <summary>
    ///     Encrypts bytes using AES-CBC with the specified key and a randomly generated IV.
    /// </summary>
    /// <param name="key">The AES encryption key.</param>
    /// <param name="plainBytes">The bytes to encrypt.</param>
    /// <returns>The IV prepended to the ciphertext.</returns>
    private byte[] Encrypt(byte[] key, byte[] plainBytes)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var cipherText = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var combined = new byte[aes.IV.Length + cipherText.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherText, 0, combined, aes.IV.Length, cipherText.Length);
        return combined;
    }

    /// <summary>
    ///     Decrypts bytes using AES-CBC with the specified key, reading the IV from the start of the input.
    /// </summary>
    /// <param name="key">The AES decryption key.</param>
    /// <param name="cipherBytes">The bytes to decrypt, with the IV prepended to the ciphertext.</param>
    /// <returns>The decrypted bytes.</returns>
    private byte[] Decrypt(byte[] key, byte[] cipherBytes)
    {
        var iv = new byte[16];
        var cipherText = new byte[cipherBytes.Length - 16];
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, iv.Length, cipherText, 0, cipherText.Length);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        return plainBytes;
    }
}
