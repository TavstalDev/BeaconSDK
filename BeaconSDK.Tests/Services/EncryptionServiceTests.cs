using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Services.Implementations;
using Tavstal.BeaconSDK.Tests.Fakes;

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
        return new EncryptionService(new CapturingBeaconLogger<EncryptionService>(), provider);
    }

    private static EncryptionService CreateFallback()
    {
        return new EncryptionService(new CapturingBeaconLogger<EncryptionService>());
    }

    [Fact(DisplayName = "Encrypt produces a six-byte prefix matching the host path")]
    public void Encrypt_ProducesSixBytePrefix_MatchingHostPath()
    {
        var keyDirectory = Path.Combine(Path.GetTempPath(), $"beacon-keys-{Guid.NewGuid():N}");
        Directory.CreateDirectory(keyDirectory);
        try
        {
            var withDp = new EncryptionService(new CapturingBeaconLogger<EncryptionService>(),
                DataProtectionProvider.Create(new DirectoryInfo(keyDirectory)));

            PrefixOf(withDp.Encrypt("payload"u8.ToArray())).Should().Be("dpsv1:");
        }
        finally
        {
            Directory.Delete(keyDirectory, true);
        }

        var fallback = CreateFallback();
        switch (OSHelper.GetOperatingSystem())
        {
            case EOperatingSystem.LINUX:
                PrefixOf(fallback.Encrypt("payload"u8.ToArray())).Should().Be("linv1:");
                break;
            case EOperatingSystem.MACOS:
                PrefixOf(fallback.Encrypt("payload"u8.ToArray())).Should().Be("macv1:");
                break;
            case EOperatingSystem.WINDOWS:
                PrefixOf(fallback.Encrypt("payload"u8.ToArray())).Should().Be("winv1:");
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

            service.Decrypt(cipher).Should().Equal(plain);
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

        service.Decrypt(cipher).Should().Equal(plain);
    }

    [Fact(DisplayName = "Decrypt passes input shorter than the prefix bytes through unchanged")]
    public void Decrypt_InputShorterThanPrefixBytes_PassesThroughUnchanged()
    {
        var service = CreateFallback();
        var input = "123456"u8.ToArray();

        service.Decrypt(input).Should().Equal(input);
    }

    [Fact(DisplayName = "Decrypt throws CryptographicException on an unknown prefix")]
    public void Decrypt_UnknownPrefix_ThrowsCryptographicException()
    {
        var service = CreateFallback();
        var input = "zzzzzzzzzz"u8.ToArray();

        var act = () => service.Decrypt(input);

        act.Should().Throw<CryptographicException>();
    }

    [Fact(DisplayName = "Decrypt returns the input for an unknown prefix when ignoreUnknown is set")]
    public void Decrypt_UnknownPrefix_WithIgnoreUnknown_ReturnsInput()
    {
        var service = CreateFallback();
        var input = "zzzzzzzzzz"u8.ToArray();

        service.Decrypt(input, true).Should().Equal(input);
    }

    [Fact(DisplayName = "Decrypt throws for the dp prefix when no provider is configured")]
    public void Decrypt_DpPrefix_WithoutProvider_Throws()
    {
        var service = CreateFallback();
        var input = "dpsv1:someciphertext"u8.ToArray();

        var act = () => service.Decrypt(input);

        act.Should().Throw<CryptographicException>();
    }

    [Fact(DisplayName = "Encrypt round-trips an empty plaintext")]
    public void Encrypt_EmptyPlaintext_RoundTrips()
    {
        var service = CreateWithDataProtection(out var keyDirectory);
        try
        {
            var cipher = service.Encrypt([]);

            service.Decrypt(cipher).Should().BeEmpty();
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

            first.Should().NotEqual(second);
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

        service.Reprotect(input).Should().Equal(input);
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

            service.Decrypt(reprotected).Should().Equal(plain);
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

            cipher.Should().Equal(original);
        }
        finally
        {
            Directory.Delete(keyDirectory, true);
        }
    }
}
