using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;

namespace Tavstal.BeaconSDK.Tests.Models.MojangApi;

public class ClassifierTests
{
    private static Classifier CreateClassifier()
    {
        return new Classifier(
            new Artifact("windows", "sha-win", 1, "https://example.com/win"),
            new Artifact("osx", "sha-osx", 1, "https://example.com/osx"),
            new Artifact("linux", "sha-linux", 1, "https://example.com/linux"));
    }

    [Fact(DisplayName = "GetOsNative matches the host operating system")]
    public void GetOsNative_MatchesHostOs()
    {
        var classifier = CreateClassifier();

        var native = classifier.GetOsNative();

        switch (OSHelper.GetOperatingSystem())
        {
            case EOperatingSystem.WINDOWS:
                Assert.Same(classifier.WindowsNatives, native);
                break;
            case EOperatingSystem.MACOS:
                Assert.Same(classifier.OsxNatives, native);
                break;
            default:
                Assert.Same(classifier.LinuxNatives, native);
                break;
        }
    }

    [Fact(DisplayName = "GetOsNative falls back to Linux when the OS is unknown")]
    public void GetOsNative_FallsBackToLinux_WhenUnknown()
    {
        var classifier = CreateClassifier();

        if (OSHelper.GetOperatingSystem() == EOperatingSystem.UNKNOWN)
            Assert.Same(classifier.LinuxNatives, classifier.GetOsNative());
        else
            Assert.Skip("Host OS is known; the UNKNOWN fallback branch cannot be exercised on this host.");
    }
}
