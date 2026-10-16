using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;
using Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;

namespace Tavstal.BeaconSDK.Tests.Models.MojangApi;

public class LibraryMetaRulesTests
{
    private static LibraryMeta Meta(params Rule[] rules)
    {
        return new LibraryMeta("net.example:lib:1.0", new LibraryDownloads(), [.. rules]);
    }

    [Fact(DisplayName = "No rules returns true")]
    public void NoRules_ReturnsTrue()
    {
        Assert.True(Meta().GetRulesResult());
    }

    [Fact(DisplayName = "Null rules returns true")]
    public void NullRules_ReturnsTrue()
    {
        var meta = new LibraryMeta { Name = "lib", Downloads = new LibraryDownloads(), Rules = null };

        Assert.True(meta.GetRulesResult());
    }

    [Fact(DisplayName = "A rule with a null OS applies its action")]
    public void RuleWithNullOs_AppliesAction()
    {
        Assert.True(Meta(new Rule { Action = "allow", Os = null }).GetRulesResult());
        Assert.False(Meta(new Rule { Action = "disallow", Os = null }).GetRulesResult());
    }

    [Fact(DisplayName = "A rule with a null OS name applies its action")]
    public void RuleWithNullOsName_AppliesAction()
    {
        Assert.True(Meta(new Rule("allow", new RuleOs())).GetRulesResult());
        Assert.False(Meta(new Rule("disallow", new RuleOs())).GetRulesResult());
    }

    [Fact(DisplayName = "An allow rule with no OS returns true")]
    public void AllowRule_NoOs_ReturnsTrue()
    {
        Assert.True(Meta(new Rule("allow", new RuleOs())).GetRulesResult());
    }

    [Fact(DisplayName = "A disallow rule with no OS returns false")]
    public void DisallowRule_NoOs_ReturnsFalse()
    {
        Assert.False(Meta(new Rule("disallow", new RuleOs())).GetRulesResult());
    }

    [Fact(DisplayName = "A Linux rule matches on a Linux host")]
    public void LinuxRule_MatchesOnLinuxHost()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: assumes a Linux CI host.");

        Assert.True(Meta(new Rule("allow", new RuleOs("linux"))).GetRulesResult());
    }

    [Fact(DisplayName = "A Windows rule is skipped on a Linux host")]
    public void WindowsRule_IsSkipped_OnLinuxHost()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: assumes a Linux CI host.");

        var meta = Meta(
            new Rule("allow", new RuleOs()),
            new Rule("disallow", new RuleOs("windows")));

        Assert.True(meta.GetRulesResult());
    }

    [Fact(DisplayName = "An osx rule only matches macOS")]
    public void OsxRule_StartsWithMatches_MacosOnly()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() != EOperatingSystem.MACOS,
            "Host-pinned: asserts a non-macOS host.");

        Assert.False(Meta(new Rule("allow", new RuleOs("osx"))).GetRulesResult());
    }

    [Fact(DisplayName = "An x86 rule matches when the host is 32-bit")]
    public void X86Rule_Matches_WhenHostIs32Bit()
    {
        Assert.SkipUnless(!OSHelper.Is64BitOperatingSystem(), "Host-pinned: requires a 32-bit host.");

        Assert.True(Meta(new Rule("allow", new RuleOs("x86"))).GetRulesResult());
    }

    [Fact(DisplayName = "An x86 rule is skipped when the host is 64-bit")]
    public void X86Rule_IsSkipped_WhenHostIs64Bit()
    {
        Assert.SkipUnless(OSHelper.Is64BitOperatingSystem(), "Host-pinned: requires a 64-bit host.");

        Assert.False(Meta(new Rule("allow", new RuleOs("x86"))).GetRulesResult());
    }

    [Fact(DisplayName = "The last matching rule wins, not the first")]
    public void LastMatchingRule_Wins_NotFirst()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: assumes a Linux CI host.");

        var meta = Meta(
            new Rule("allow", new RuleOs("linux")),
            new Rule("disallow", new RuleOs("linux")));

        Assert.False(meta.GetRulesResult());
    }

    [Fact(DisplayName = "Disallowed by default when no rule matches")]
    public void DisallowedByDefault_WhenNoRuleMatches()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: assumes a Linux CI host.");

        var meta = Meta(
            new Rule("allow", new RuleOs("windows")),
            new Rule("allow", new RuleOs("osx")));

        Assert.False(meta.GetRulesResult());
    }
}
