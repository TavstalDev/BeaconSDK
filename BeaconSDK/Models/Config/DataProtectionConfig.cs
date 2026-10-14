using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config;

/// <summary>
///     Represents the data protection settings used by <see cref="Tavstal.BeaconSDK.Services.Implementations.EncryptionService" />
///     and the data protection key ring persistence configured in <see cref="Tavstal.BeaconSDK.BeaconBootstrap" />.
/// </summary>
public class DataProtectionConfig
{
    /// <summary>
    ///     Gets or sets the directory where the data protection key ring is persisted. Defaults to <c>keysDir</c>.
    /// </summary>
    [JsonPropertyName("KeysDirectory")]
    public string KeysDirectory { get; set; } = "keysDir";

    /// <summary>
    ///     Gets or sets the application name used to isolate the data protection key ring. Defaults to <c>myApplication</c>.
    /// </summary>
    [JsonPropertyName("ApplicationName")]
    public string ApplicationName { get; set; } = "myApplication";

    /// <summary>
    ///     Gets or sets the purpose string passed to the data protector. Defaults to <c>BeaconSDK.Encryption.v1</c>.
    /// </summary>
    [JsonPropertyName("ProtectorPurpose")]
    public string ProtectorPurpose { get; set; } = "BeaconSDK.Encryption.v1";

    /// <summary>
    ///     Gets or sets the optional entropy appended when protecting data with Windows DPAPI.
    /// </summary>
    [JsonPropertyName("WindowsEntropy")]
    public byte[] WindowsEntropy { get; set; } = [.. "8AE1C425-241D-46A9-A8F8-6E8A68D2586C"u8];

    /// <summary>
    ///     Gets or sets the AES key used by the fallback encryption when no data protection provider is configured on Linux.
    /// </summary>
    [JsonPropertyName("LinuxKey")]
    public byte[] LinuxKey { get; set; } = [.. "5aEcj3cLUm5qYeCpptEaQoFCUTgZyEZw"u8];

    /// <summary>
    ///     Gets or sets the AES key used by the fallback encryption when no data protection provider is configured on macOS.
    /// </summary>
    [JsonPropertyName("MacKey")]
    public byte[] MacKey { get; set; } = [.. "MqcM2kVeroKpvh9z79t3KjtiTzCUKSRs"u8];
}
