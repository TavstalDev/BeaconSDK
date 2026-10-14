namespace Tavstal.BeaconSDK.Models.Instance.Config;

/// <summary>
///     Represents the details of various paths used in the launcher.
/// </summary>
public class PathDetails
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PathDetails" /> class with the specified parameters.
    /// </summary>
    /// <param name="assetsDir">The directory path for assets.</param>
    /// <param name="cacheDir">The directory path for cached files.</param>
    /// <param name="librariesDir">The directory path for libraries.</param>
    /// <param name="versionsDir">The directory path for versions.</param>
    /// <param name="customNativeFiles">The list of custom native files.</param>
    public PathDetails(string assetsDir, string cacheDir, string librariesDir, string versionsDir, List<string> customNativeFiles)
    {
        AssetsDir = assetsDir;
        CacheDir = cacheDir;
        LibrariesDir = librariesDir;
        VersionsDir = versionsDir;
        CustomNativeFiles = customNativeFiles;
    }

    /// <summary>
    ///     Gets or sets the directory path for assets.
    /// </summary>
    public string AssetsDir { get; set; }

    /// <summary>
    ///     Gets or sets the directory path for cached files.
    /// </summary>
    public string CacheDir { get; set; }

    /// <summary>
    ///     Gets or sets the directory path for libraries.
    /// </summary>
    public string LibrariesDir { get; set; }

    /// <summary>
    ///     Gets or sets the directory path for versions.
    /// </summary>
    public string VersionsDir { get; set; }

    /// <summary>
    ///     Gets or sets the list of custom native files.
    /// </summary>
    public List<string> CustomNativeFiles { get; set; }
}
