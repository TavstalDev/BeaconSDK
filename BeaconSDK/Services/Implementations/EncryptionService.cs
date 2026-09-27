using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Services.Implementations;

/// <inheritdoc/>
public class EncryptionService : IEncryptionService
{
    private const string DP_PREFIX = "dpv1:";
    private const string WIN_PREFIX = "winv1:";
    private const string LINUX_PREFIX = "linuxv1:";
    private const string MAC_PREFIX   = "macv1:";
    private readonly byte[] s_entropy = [.. "8AE1C425-241D-46A9-A8F8-6E8A68D2586C"u8];
    private const string linuxKey = "898c3f038bfc090b62a32156aba62acda193e90e26baf0ab";
    private const string macKey = "a83231f9eadeac1e8a3b875d86da77a43e911180d9e88968";

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionService"/> class.
    /// </summary>
    /// <param name="dataProtector">
    /// The data protection protector used to encrypt and decrypt values. When <see langword="null"/>,
    /// platform-specific encryption is used instead.
    /// </param>
    public EncryptionService(IDataProtector? dataProtector)
    {
        DataProtector = dataProtector;
    }

    /// <inheritdoc/>
    public IDataProtector? DataProtector { get; set; }
    
    /// <inheritdoc/>
    public string Encrypt(string text)
    {
        if (DataProtector != null)
            return DP_PREFIX + DataProtector.Protect(text);
            
        switch (OSHelper.GetOperatingSystem())
        {
            case EOperatingSystem.WINDOWS:
                return WIN_PREFIX + EncryptWin(text);
            case EOperatingSystem.LINUX:
                return LINUX_PREFIX + EncryptLinux(text);
            case EOperatingSystem.MACOS:
                return MAC_PREFIX + EncryptMac(text);
            default:
                throw new Exception("Unknown operating system");
        }
    }

    /// <inheritdoc/>
    public string Decrypt(string text, bool ignoreUnknown = false)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (text.StartsWith(DP_PREFIX))
        {
            if (DataProtector == null)
                throw new CryptographicException("Data protector is not set for DP encrypted text");
            return DataProtector.Unprotect(text[DP_PREFIX.Length..]);
        }

        if (text.StartsWith(WIN_PREFIX))
            return DecryptWin(text[WIN_PREFIX.Length..]);

        if (text.StartsWith(LINUX_PREFIX))
            return DecryptLinux(text[LINUX_PREFIX.Length..]);
            
        if (text.StartsWith(MAC_PREFIX))
            return DecryptMac(text[MAC_PREFIX.Length..]);

        if (ignoreUnknown)
            return text;
        throw new CryptographicException("Unknown encryption format");
    }

    /// <inheritdoc/>
    public string Reprotect(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted)) 
            return encrypted;

        try
        {
            if (DataProtector != null)
            {
                // if prefix is DP, decrypt & re-protect with current provider
                if (encrypted.StartsWith(DP_PREFIX))
                    return DP_PREFIX + DataProtector.Protect(DataProtector.Unprotect(encrypted[DP_PREFIX.Length..]));
                if (encrypted.StartsWith(WIN_PREFIX))
                {
                    // else if prefix is WIN, decrypt using Windows, then protect with DP
                    var plain = DecryptWin(encrypted[WIN_PREFIX.Length..]);
                    return DP_PREFIX + DataProtector.Protect(plain);
                }

                if (encrypted.StartsWith(LINUX_PREFIX))
                {
                    // else if prefix is LINUX, decrypt using Linux, then protect with DP
                    var plain = DecryptLinux(encrypted[LINUX_PREFIX.Length..]);
                    return DP_PREFIX + DataProtector.Protect(plain);
                }

                if (encrypted.StartsWith(MAC_PREFIX))
                {
                    // else if prefix is MAC, decrypt using Mac, then protect with DP
                    var plain = DecryptMac(encrypted[MAC_PREFIX.Length..]);
                    return DP_PREFIX + DataProtector.Protect(plain);
                }

                // Not known format, protect as-is
                return DP_PREFIX + DataProtector.Protect(encrypted);
            }

            return encrypted; // fallback: keep as-is
        }
        catch
        {
            return encrypted; // fail-safe
        }
    }
    
    /// <summary>
    /// Encrypts text using Windows-specific encryption.
    /// </summary>
    /// <param name="text">The text to encrypt.</param>
    /// <returns>The encrypted text.</returns>
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private string EncryptWin(string text)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(text);
        byte[] encryptedBytes = ProtectedData.Protect(plainBytes, s_entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypts text using Windows-specific decryption.
    /// </summary>
    /// <param name="text">The text to decrypt.</param>
    /// <returns>The decrypted text.</returns>
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private string DecryptWin(string text)
    {
        byte[] cipherBytes = Convert.FromBase64String(text);
        byte[] decryptedBytes = ProtectedData.Unprotect(cipherBytes, s_entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    /// <summary>
    /// Encrypts text using Linux-specific encryption.
    /// </summary>
    /// <param name="text">The text to encrypt.</param>
    /// <returns>The encrypted text.</returns>
    private string EncryptLinux(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        // This is a fallback method for encryption
        // in case of IDataProtector is not set.
        // It is recommended to set IDataProtector for better security.
        // However, if not set, this method will be used.
        // This is not the most secure method, especially with a static key,
        // but it is better than nothing.
        // I could generate a custom key on each machine, but since I would need to store it
        // somewhere unencrypted, it would defeat the purpose of encryption.
        return Encrypt(linuxKey, text);
    }

    /// <summary>
    /// Decrypts text using Linux-specific decryption.
    /// </summary>
    /// <param name="text">The text to decrypt.</param>
    /// <returns>The decrypted text.</returns>
    private string DecryptLinux(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        // This is a fallback method for encryption
        // in case of IDataProtector is not set.
        // It is recommended to set IDataProtector for better security.
        // However, if not set, this method will be used.
        // This is not the most secure method, especially with a static key,
        // but it is better than nothing.
        // I could generate a custom key on each machine, but since I would need to store it
        // somewhere unencrypted, it would defeat the purpose of encryption.
        return Decrypt(linuxKey, text);
    }

    /// <summary>
    /// Encrypts text using macOS-specific encryption.
    /// </summary>
    /// <param name="text">The text to encrypt.</param>
    /// <returns>The encrypted text.</returns>
    private string EncryptMac(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        // This is a fallback method for encryption
        // in case of IDataProtector is not set.
        // It is recommended to set IDataProtector for better security.
        // However, if not set, this method will be used.
        // This is not the most secure method, especially with a static key,
        // but it is better than nothing.
        // I could generate a custom key on each machine, but since I would need to store it
        // somewhere unencrypted, it would defeat the purpose of encryption.
        return Encrypt(macKey, text);
    }

    /// <summary>
    /// Decrypts text using macOS-specific decryption.
    /// </summary>
    /// <param name="text">The text to decrypt.</param>
    /// <returns>The decrypted text.</returns>
    private string DecryptMac(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        // This is a fallback method for encryption
        // in case of IDataProtector is not set.
        // It is recommended to set IDataProtector for better security.
        // However, if not set, this method will be used.
        // This is not the most secure method, especially with a static key,
        // but it is better than nothing.
        // I could generate a custom key on each machine, but since I would need to store it
        // somewhere unencrypted, it would defeat the purpose of encryption.
        return Decrypt(macKey, text);
    }
    
    /// <summary>
    /// Encrypts text using AES encryption with the specified key.
    /// </summary>
    /// <param name="key">The encryption key.</param>
    /// <param name="plainText">The text to encrypt.</param>
    /// <returns>The encrypted text.</returns>
    private string Encrypt(string key, string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.GenerateIV();

        using ICryptoTransform encryptor = aes.CreateEncryptor();

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherText = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        byte[] combined = new byte[aes.IV.Length + cipherText.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherText, 0, combined, aes.IV.Length, cipherText.Length);

        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Decrypts text using AES decryption with the specified key.
    /// </summary>
    /// <param name="key">The decryption key.</param>
    /// <param name="base64String">The text to decrypt.</param>
    /// <returns>The decrypted text.</returns>
    private string Decrypt(string key, string base64String)
    {
        byte[] buffer = Convert.FromBase64String(base64String);
        byte[] iv = new byte[16];
        byte[] cipherText = new byte[buffer.Length - 16];
        Buffer.BlockCopy(buffer, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(buffer, iv.Length, cipherText, 0, cipherText.Length);
        
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = iv;
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}