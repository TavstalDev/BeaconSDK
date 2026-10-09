using System.Text.Json.Serialization;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;

namespace Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;

/// <summary>
///     Represents a classifier for native libraries, including platform-specific artifacts for Windows, macOS, and Linux.
/// </summary>
public class Classifier
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Classifier" /> class.
    /// </summary>
    public Classifier()
    {
        WindowsNatives = new Artifact();
        OsxNatives = new Artifact();
        LinuxNatives = new Artifact();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Classifier" /> class with specified platform-specific artifacts.
    /// </summary>
    /// <param name="windowsNatives">The artifact for Windows native libraries.</param>
    /// <param name="osxNatives">The artifact for macOS native libraries.</param>
    /// <param name="linuxNatives">The artifact for Linux native libraries.</param>
    public Classifier(Artifact windowsNatives, Artifact osxNatives, Artifact linuxNatives)
    {
        WindowsNatives = windowsNatives;
        OsxNatives = osxNatives;
        LinuxNatives = linuxNatives;
    }

    /// <summary>
    ///     Gets or sets the artifact for Windows native libraries.
    /// </summary>
    [JsonPropertyName("natives-windows")]
    public Artifact WindowsNatives { get; set; }

    /// <summary>
    ///     Gets or sets the artifact for macOS native libraries.
    /// </summary>
    [JsonPropertyName("natives-osx")]
    public Artifact OsxNatives { get; set; }

    /// <summary>
    ///     Gets or sets the artifact for Linux native libraries.
    /// </summary>
    [JsonPropertyName("natives-linux")]
    public Artifact LinuxNatives { get; set; }

    /// <summary>
    ///     Retrieves the native library artifact for the current operating system.
    /// </summary>
    /// <returns>
    ///     The <see cref="Artifact" /> object corresponding to the native library for the detected operating system.
    /// </returns>
    public Artifact GetOsNative()
    {
        switch (OSHelper.GetOperatingSystem())
        {
            case EOperatingSystem.WINDOWS:
            {
                return WindowsNatives;
            }
            case EOperatingSystem.MACOS:
            {
                return OsxNatives;
            }
            default:
            case EOperatingSystem.LINUX:
            case EOperatingSystem.UNKNOWN:
            {
                return LinuxNatives;
            }
        }
    }
}
