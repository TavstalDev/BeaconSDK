using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Services.Implementations;
using Tavstal.BeaconSDK.Tests.Fakes;
using Tavstal.BeaconSDK.Tests.Fixtures;

namespace Tavstal.BeaconSDK.Tests.Services;

public class EncryptionServiceTests
{
    private static string PrefixOf(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes, 0, 6);
    }

    private static EncryptionService CreateWithDataProtection(out string keyDirectory)
    {
        keyDirectory = Path.Combine(Path.GetTempPath(), $"beacon-keys-{Guid.NewGuid():N}");
        Directory.CreateDirectory(keyDirectory);
        var provider = DataProtectionProvider.Create(new DirectoryInfo(keyDirectory));
        return new EncryptionService(new CapturingBeaconLogger<EncryptionService>(), ModelFactory.CreateAppConfiguration(),  provider);
    }

    private static EncryptionService CreateFallback()
    {
        return new EncryptionService(new CapturingBeaconLogger<EncryptionService>(), ModelFactory.CreateAppConfiguration());
    }

    [Fact(DisplayName = "Encrypt produces a six-byte prefix matching the host path")]
    public void Encrypt_ProducesSixBytePrefix_MatchingHostPath()
    {
        var keyDirectory = Path.Combine(Path.GetTempPath(), $"beacon-keys-{Guid.NewGuid():N}");
        Directory.CreateDirectory(keyDirectory);
        try
        {
            var config = ModelFactory.CreateAppConfiguration();
            config.DataProtection.KeysDirectory = keyDirectory;
            var withDp = new EncryptionService(new CapturingBeaconLogger<EncryptionService>(), config, DataProtectionProvider.Create(new DirectoryInfo(keyDirectory)));

            Assert.Equal("dpsv1:", PrefixOf(withDp.Encrypt("payload"u8.ToArray())));
        }
        finally
        {
            Directory.Delete(keyDirectory, true);
        }

        var fallback = CreateFallback();
        switch (OSHelper.GetOperatingSystem())
        {
            case EOperatingSystem.LINUX:
                Assert.Equal("linv1:", PrefixOf(fallback.Encrypt("payload"u8.ToArray())));
                break;
            case EOperatingSystem.MACOS:
                Assert.Equal("macv1:", PrefixOf(fallback.Encrypt("payload"u8.ToArray())));
                break;
            case EOperatingSystem.WINDOWS:
                Assert.Equal("winv1:", PrefixOf(fallback.Encrypt("payload"u8.ToArray())));
                break;
            default:
                Assert.Skip("Host OS has no fallback encryption path.");
                break;
        }
    }

    [Fact(DisplayName = "Encrypt and decrypt round-trip with the data protection provider")]
    public void EncryptDecrypt_RoundTrips_WithDataProvider()
    {
        var service = CreateWithDataProtection(out var keyDirectory);
        try
        {
            var plain = "a-secret-access-token"u8.ToArray();

            var cipher = service.Encrypt(plain);

            Assert.Equal(plain, service.Decrypt(cipher));
        }
        finally
        {
            Directory.Delete(keyDirectory, true);
        }
    }

    [Fact(DisplayName = "Encrypt and decrypt round-trip through the AES fallback on Linux")]
    public void EncryptDecrypt_RoundTrips_AesFallback_OnLinux()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: the AES fallback test targets the Linux path.");

        var service = CreateFallback();
        var plain = "a-secret-access-token"u8.ToArray();

        var cipher = service.Encrypt(plain);

        Assert.Equal(plain, service.Decrypt(cipher));
    }

    [Fact(DisplayName = "Decrypt passes input shorter than the prefix bytes through unchanged")]
    public void Decrypt_InputShorterThanPrefixBytes_PassesThroughUnchanged()
    {
        var service = CreateFallback();
        var input = "123456"u8.ToArray();

        Assert.Equal(input, service.Decrypt(input));
    }

    [Fact(DisplayName = "Decrypt throws CryptographicException on an unknown prefix")]
    public void Decrypt_UnknownPrefix_ThrowsCryptographicException()
    {
        var service = CreateFallback();
        var input = "zzzzzzzzzz"u8.ToArray();

        var act = () => service.Decrypt(input);

        Assert.Throws<CryptographicException>(act);
    }

    [Fact(DisplayName = "Decrypt returns the input for an unknown prefix when ignoreUnknown is set")]
    public void Decrypt_UnknownPrefix_WithIgnoreUnknown_ReturnsInput()
    {
        var service = CreateFallback();
        var input = "zzzzzzzzzz"u8.ToArray();

        Assert.Equal(input, service.Decrypt(input, true));
    }

    [Fact(DisplayName = "Decrypt throws for the dp prefix when no provider is configured")]
    public void Decrypt_DpPrefix_WithoutProvider_Throws()
    {
        var service = CreateFallback();
        var input = "dpsv1:someciphertext"u8.ToArray();

        var act = () => service.Decrypt(input);

        Assert.Throws<CryptographicException>(act);
    }

    [Fact(DisplayName = "Encrypt round-trips an empty plaintext")]
    public void Encrypt_EmptyPlaintext_RoundTrips()
    {
        var service = CreateWithDataProtection(out var keyDirectory);
        try
        {
            var cipher = service.Encrypt([]);

            Assert.Empty(service.Decrypt(cipher));
        }
        finally
        {
            Directory.Delete(keyDirectory, true);
        }
    }

    [Fact(DisplayName = "Encrypt produces different ciphertexts due to a random IV")]
    public void Encrypt_ProducesDifferentCiphertexts_RandomIv()
    {
        var service = CreateWithDataProtection(out var keyDirectory);
        try
        {
            var plain = "same-input-every-time"u8.ToArray();

            var first = service.Encrypt(plain);
            var second = service.Encrypt(plain);

            Assert.NotEqual(second, first);
        }
        finally
        {
            Directory.Delete(keyDirectory, true);
        }
    }

    [Fact(DisplayName = "Reprotect returns unencrypted input unchanged")]
    public void Reprotect_WithUnencryptedInput_ReturnsInputUnchanged()
    {
        var service = CreateFallback();
        var input = "plain-and-unknown-format"u8.ToArray();

        Assert.Equal(input, service.Reprotect(input));
    }

    [Fact(DisplayName = "Reprotect transforms encrypted input and round-trips")]
    public void Reprotect_TransformsEncryptedInput_RoundTrip()
    {
        var service = CreateWithDataProtection(out var keyDirectory);
        try
        {
            var plain = "rotate-me"u8.ToArray();
            var cipher = service.Encrypt(plain);

            var reprotected = service.Reprotect(cipher);

            Assert.Equal(plain, service.Decrypt(reprotected));
        }
        finally
        {
            Directory.Delete(keyDirectory, true);
        }
    }

    [Fact(DisplayName = "Decrypt does not mutate the caller's input array")]
    public void Decrypt_DoesNotMutateCallerInputArray()
    {
        var service = CreateWithDataProtection(out var keyDirectory);
        try
        {
            var cipher = service.Encrypt([.. "immutable-input"u8]);
            var original = cipher.ToArray();

            service.Decrypt(cipher);

            Assert.Equal(original, cipher);
        }
        finally
        {
            Directory.Delete(keyDirectory, true);
        }
    }
}
