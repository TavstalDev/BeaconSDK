using Tavstal.BeaconSDK.Services.Abstractions;

namespace Tavstal.BeaconSDK.Tests.Fakes;

/// <summary>
///     A deterministic <see cref="IEncryptionService" /> that records how often each
///     operation was invoked and prefixes/looks-for a stable marker.
/// </summary>
public sealed class RecordingEncryptionService : IEncryptionService
{
    private static readonly byte[] Prefix = "testv:"u8.ToArray();

    public int EncryptCallCount { get; private set; }
    public int DecryptCallCount { get; private set; }
    public int ReprotectCallCount { get; private set; }

    public bool FailReprotect { get; set; }

    public byte[] Encrypt(byte[] plainBytes)
    {
        EncryptCallCount++;
        return [.. Prefix, .. plainBytes];
    }

    public byte[] Decrypt(byte[] cipherBytes, bool ignoreUnknown = false)
    {
        DecryptCallCount++;
        if (cipherBytes.Length < Prefix.Length ||
            !cipherBytes.AsSpan(0, Prefix.Length).SequenceEqual(Prefix))
        {
            if (ignoreUnknown)
                return cipherBytes;
            throw new System.Security.Cryptography.CryptographicException("Unknown encryption format.");
        }

        return cipherBytes[Prefix.Length..];
    }

    public byte[] Reprotect(byte[] cipherBytes)
    {
        ReprotectCallCount++;
        if (FailReprotect)
            return cipherBytes;

        return [.. Prefix, .. cipherBytes];
    }
}
