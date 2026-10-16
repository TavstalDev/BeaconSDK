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
    public required byte[] WindowsEntropy { get; set; }

    /// <summary>
    ///     Gets or sets the AES key used by the fallback encryption when no data protection provider is configured on Linux.
    /// </summary>
    [JsonPropertyName("LinuxKey")]
    public required byte[] LinuxKey { get; set; }

    /// <summary>
    ///     Gets or sets the AES key used by the fallback encryption when no data protection provider is configured on macOS.
    /// </summary>
    [JsonPropertyName("MacKey")]
    public required byte[] MacKey { get; set; }
}
