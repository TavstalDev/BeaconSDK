using System.Text.Json.Serialization;
using Tavstal.BeaconSDK.Enums;

namespace Tavstal.BeaconSDK.Models.Java;

/// <summary>
///     Represents the configuration for Java mirrors, mapping each operating system to the list of
///     available <see cref="JavaMirror" /> downloads.
/// </summary>
public class JavaMirrorConfig
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="JavaMirrorConfig" /> class populated with the built-in
    ///     default mirrors for Windows, Linux and macOS.
    /// </summary>
    public JavaMirrorConfig()
    {
        Mirrors = new Dictionary<EOperatingSystem, List<JavaMirror>>()
        {
            {
                EOperatingSystem.WINDOWS,
                [
                    new JavaMirror
                    {
                        Version = 8,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin8-binaries/releases/download/jdk8u462-b08/OpenJDK8U-jdk_x64_windows_hotspot_8u462b08.zip"
                    },
                    new JavaMirror
                    {
                        Version = 16,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin16-binaries/releases/download/jdk-16.0.2%2B7/OpenJDK16U-jdk_x64_windows_hotspot_16.0.2_7.zip"
                    },
                    new JavaMirror
                    {
                        Version = 17,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.9%2B9/OpenJDK17U-jdk_x86-32_windows_hotspot_17.0.9_9.zip"
                    },
                    new JavaMirror
                    {
                        Version = 21,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.8%2B9/OpenJDK21U-jdk_x64_windows_hotspot_21.0.8_9.zip"
                    },
                    new JavaMirror
                    {
                        Version = 25,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin25-binaries/releases/download/jdk-25.0.2%2B10/OpenJDK25U-jdk_x64_windows_hotspot_25.0.2_10.zip"
                    }
                ]
            },
            {
                EOperatingSystem.LINUX,
                [
                    new JavaMirror
                    {
                        Version = 8,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin8-binaries/releases/download/jdk8u462-b08/OpenJDK8U-jdk_x64_linux_hotspot_8u462b08.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 16,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin16-binaries/releases/download/jdk-16.0.2%2B7/OpenJDK16U-jdk_x64_linux_hotspot_16.0.2_7.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 17,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.9%2B9/OpenJDK17U-jdk_x64_linux_hotspot_17.0.9_9.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 21,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.8%2B9/OpenJDK21U-jdk_x64_linux_hotspot_21.0.8_9.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 25,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin25-binaries/releases/download/jdk-25.0.2%2B10/OpenJDK25U-jdk_x64_linux_hotspot_25.0.2_10.tar.gz"
                    }
                ]
            },
            {
                EOperatingSystem.MACOS,
                [
                    new JavaMirror
                    {
                        Version = 8,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin8-binaries/releases/download/jdk8u462-b08/OpenJDK8U-jdk_x64_mac_hotspot_8u462b08.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 16,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin16-binaries/releases/download/jdk-16.0.2%2B7/OpenJDK16U-jdk_x64_mac_hotspot_16.0.2_7.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 17,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.9%2B9/OpenJDK17U-jdk_x64_mac_hotspot_17.0.9_9.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 17,
                        IsArm = true,
                        Url = "https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.9%2B9/OpenJDK17U-jdk_aarch64_mac_hotspot_17.0.9_9.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 21,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.8%2B9/OpenJDK21U-jdk_x64_mac_hotspot_21.0.8_9.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 21,
                        IsArm = true,
                        Url = "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.8%2B9/OpenJDK21U-jdk_aarch64_mac_hotspot_21.0.8_9.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 25,
                        IsArm = false,
                        Url = "https://github.com/adoptium/temurin25-binaries/releases/download/jdk-25.0.2%2B10/OpenJDK25U-jdk_x64_mac_hotspot_25.0.2_10.tar.gz"
                    },
                    new JavaMirror
                    {
                        Version = 25,
                        IsArm = true,
                        Url = "https://github.com/adoptium/temurin25-binaries/releases/download/jdk-25.0.2%2B10/OpenJDK25U-jdk_aarch64_mac_hotspot_25.0.2_10.tar.gz"
                    }
                ]
            }
        };
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="JavaMirrorConfig" /> class with the specified mirrors.
    /// </summary>
    /// <param name="mirrors">The mirrors to use, keyed by operating system.</param>
    public JavaMirrorConfig(Dictionary<EOperatingSystem, List<JavaMirror>> mirrors)
    {
        Mirrors = mirrors;
    }

    /// <summary>
    ///     Gets or sets the Java mirrors, keyed by the operating system they apply to.
    /// </summary>
    [JsonPropertyName("Mirrors")]
    public Dictionary<EOperatingSystem, List<JavaMirror>> Mirrors { get; set; }
}
