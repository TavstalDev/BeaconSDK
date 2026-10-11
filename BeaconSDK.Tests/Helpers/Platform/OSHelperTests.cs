using FluentAssertions;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;

namespace Tavstal.BeaconSDK.Tests.Helpers.Platform;

public class OSHelperTests
{
    [Fact(DisplayName = "GetHomeDirectory returns the user profile on Windows")]
    public void GetHomeDirectory_WindowsBranch_ReturnsUserProfile()
    {
        var result = OSHelper.GetHomeDirectory(EOperatingSystem.WINDOWS);
        result.Should().Be(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    [Fact(DisplayName = "GetHomeDirectory reads the HOME environment variable on non-Windows")]
    public void GetHomeDirectory_NonWindows_ReadsHomeEnv()
    {
        string expected = Path.Combine(Path.GetTempPath(), "beacon-home-" + Guid.NewGuid().ToString("N"));
        string? original = Environment.GetEnvironmentVariable("HOME");
        try
        {
            Environment.SetEnvironmentVariable("HOME", expected);
            OSHelper.GetHomeDirectory(EOperatingSystem.LINUX).Should().Be(expected);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOME", original);
        }
    }

    [Fact(DisplayName = "GetDesktopDirectory reads the XDG desktop directory on Linux")]
    public void GetDesktopDirectory_LinuxBranch_ReadsXdgUserDirsFile()
    {
        string expected = Path.Combine(Path.GetTempPath(), "beacon-desktop-" + Guid.NewGuid().ToString("N"));
        string? original = Environment.GetEnvironmentVariable("XDG_DESKTOP_DIR");
        try
        {
            Environment.SetEnvironmentVariable("XDG_DESKTOP_DIR", expected);
            OSHelper.GetDesktopDirectory(EOperatingSystem.LINUX).Should().Be(expected);
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DESKTOP_DIR", original);
        }
    }

    [Fact(DisplayName = "GetProgramsDirectory returns Applications on macOS and local share applications on Linux")]
    public void GetProgramsDirectory_Macos_ReturnsApplications_Linux_ReturnsLocalShareApplications()
    {
        OSHelper.GetProgramsDirectory(EOperatingSystem.MACOS).Should().Be("/Applications");

        string expected = Path.Combine(OSHelper.GetHomeDirectory(EOperatingSystem.LINUX), ".local", "share",
            "applications");
        OSHelper.GetProgramsDirectory(EOperatingSystem.LINUX).Should().Be(expected);
    }

    [Theory(DisplayName = "GetPlatformLibraryExtension returns dll/so/dylib per OS")]
    [InlineData(EOperatingSystem.WINDOWS, "dll")]
    [InlineData(EOperatingSystem.LINUX, "so")]
    [InlineData(EOperatingSystem.MACOS, "dylib")]
    public void GetPlatformLibraryExtension_ReturnsDllSoDylib_PerOs(EOperatingSystem os, string expected)
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == os, $"Host operating system is not {os}.");
        OSHelper.GetPlatformLibraryExtension().Should().Be(expected);
    }

    [Fact(DisplayName = "GetOperatingSystem on the host is not unknown")]
    public void GetOperatingSystem_OnHost_IsNotUnknown()
    {
        OSHelper.GetOperatingSystem().Should().NotBe(EOperatingSystem.UNKNOWN);
    }
}
