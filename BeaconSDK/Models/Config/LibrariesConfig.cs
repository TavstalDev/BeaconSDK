using System.Text.Json.Serialization;

namespace Tavstal.BeaconSDK.Models.Config;

/// <summary>
///     Provides the download locations of support files that are no longer embedded in the assembly,
///     such as the launch wrapper and the legacy Forge libraries.
/// </summary>
public class LibrariesConfig
{
    /// <summary>
    ///     Gets or sets the download entry for the launch wrapper jar used by vanilla, Fabric and Quilt instances.
    ///     The SHA1 hash is used to verify the download and to reuse an already downloaded copy.
    /// </summary>
    [JsonPropertyName("LaunchWrapper")]
    public FileEntry LaunchWrapper { get; set; } = new()
    {
        Url = "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/launchWrapper-1.0.jar",
        Sha1 = "5c34a47970650d334f35c7b7b62c2491004e5dc0"
    };

    /// <summary>
    ///     Gets or sets the legacy Forge support libraries keyed by file name.
    ///     The keys match the values returned by <see cref="Instances.ForgeInstance.GetLegacyLibraries" />.
    ///     Libraries that are missing locally are downloaded from the configured URL and verified with the SHA1 hash.
    /// </summary>
    [JsonPropertyName("Forge")]
    public Dictionary<string, FileEntry> Forge { get; set; } = new()
    {
        { "argo-2.25.jar", new FileEntry { Url = "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/argo-2.25.jar", Sha1 = "bb672829fde76cb163004752b86b0484bd0a7f4b"} },
        { "argo-small-3.2.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/argo-small-3.2.jar", Sha1 = "58912ea2858d168c50781f956fa5b59f0f7c6b51"} },
        { "asm-all-4.0.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/asm-all-4.0.jar", Sha1 = "98308890597acb64047f7e896638e0d98753ae82"} },
        { "asm-all-4.1.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/asm-all-4.1.jar", Sha1 = "054986e962b88d8660ae4566475658469595ef58"} },
        { "bcprov-jdk15on-147.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/bcprov-jdk15on-147.jar", Sha1 = "b6f5d9926b0afbde9f4dbe3db88c5247be7794bb"} },
        { "bcprov-jdk15on-148.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/bcprov-jdk15on-148.jar", Sha1 = "960dea7c9181ba0b17e8bab0c06a43f0a5f04e65"} },
        { "deobfuscation_data_1.5.zip", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/deobfuscation_data_1.5.zip", Sha1 = "5f7c142d53776f16304c0bbe10542014abad6af8"} },
        { "deobfuscation_data_1.5.1.zip", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/deobfuscation_data_1.5.1.zip", Sha1 = "22e221a0d89516c1f721d6cab056a7e37471d0a6"} },
        { "deobfuscation_data_1.5.2.zip", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/deobfuscation_data_1.5.2.zip", Sha1 = "446e55cd986582c70fcf12cb27bc00114c5adfd9"} },
        { "guava-12.0.1.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/guava-12.0.1.jar", Sha1 = "b8e78b9af7bf45900e14c6f958486b6ca682195f"} },
        { "guava-14.0-rc3.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/guava-14.0-rc3.jar", Sha1 = "931ae21fa8014c3ce686aaa621eae565fefb1a6a"} },
        { "scala-library.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/scala-library.jar", Sha1 = "458d046151ad179c85429ed7420ffb1eaf6ddf85"} },
    };
}
