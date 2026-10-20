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
    /// </summary>
    [JsonPropertyName("LaunchWrapper")]
    public FileEntry LaunchWrapper { get; set; } = new FileEntry
    {
        Url = "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/launchWrapper-1.0.jar",
        Sha1 = "5c34a47970650d334f35c7b7b62c2491004e5dc0"
    };

    /// <summary>
    ///     Gets or sets the legacy Forge support libraries keyed by file name.
    ///     The keys match the values returned by <see cref="Instances.ForgeInstance.GetLegacyLibraries" />.
    /// </summary>
    [JsonPropertyName("Forge")]
    public Dictionary<string, FileEntry> Forge { get; set; } = new()
    {
        { "argo-2.25.jar", new FileEntry { Url = "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/argo-2.25.jar", Sha1 = ""} },
        { "argo-small-3.2.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/argo-small-3.2.jar", Sha1 = ""} },
        { "asm-all-4.0.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/asm-all-4.0.jar", Sha1 = ""} },
        { "asm-all-4.1.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/asm-all-4.1.jar", Sha1 = ""} },
        { "bcprov-jdk15on-147.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/bcprov-jdk15on-147.jar", Sha1 = ""} },
        { "bcprov-jdk15on-148.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/bcprov-jdk15on-148.jar", Sha1 = ""} },
        { "guava-12.0.1.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/guava-12.0.1.jar", Sha1 = ""} },
        { "guava-14.0-rc3.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/guava-14.0-rc3.jar", Sha1 = ""} },
        { "scala-library.jar", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/scala-library.jar", Sha1 = ""} },
        { "deobfuscation_data_1.5.zip", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/deobfuscation_data_1.5.zip", Sha1 = ""} },
        { "deobfuscation_data_1.5.1.zip", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/deobfuscation_data_1.5.1.zip", Sha1 = ""} },
        { "deobfuscation_data_1.5.2.zip", new FileEntry { Url =  "https://github.com/TavstalDev/BeaconSDK/raw/refs/heads/master/assets/fmlLib/deobfuscation_data_1.5.2.zip", Sha1 = ""} },
    };
}
