using FluentAssertions;
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
        Meta().GetRulesResult().Should().BeTrue();
    }

    [Fact(DisplayName = "Null rules returns true")]
    public void NullRules_ReturnsTrue()
    {
        var meta = new LibraryMeta { Name = "lib", Downloads = new LibraryDownloads(), Rules = null };

        meta.GetRulesResult().Should().BeTrue();
    }

    [Fact(DisplayName = "A rule with a null OS applies its action")]
    public void RuleWithNullOs_AppliesAction()
    {
        Meta(new Rule { Action = "allow", Os = null }).GetRulesResult().Should().BeTrue();
        Meta(new Rule { Action = "disallow", Os = null }).GetRulesResult().Should().BeFalse();
    }

    [Fact(DisplayName = "A rule with a null OS name applies its action")]
    public void RuleWithNullOsName_AppliesAction()
    {
        Meta(new Rule("allow", new RuleOs())).GetRulesResult().Should().BeTrue();
        Meta(new Rule("disallow", new RuleOs())).GetRulesResult().Should().BeFalse();
    }

    [Fact(DisplayName = "An allow rule with no OS returns true")]
    public void AllowRule_NoOs_ReturnsTrue()
    {
        Meta(new Rule("allow", new RuleOs())).GetRulesResult().Should().BeTrue();
    }

    [Fact(DisplayName = "A disallow rule with no OS returns false")]
    public void DisallowRule_NoOs_ReturnsFalse()
    {
        Meta(new Rule("disallow", new RuleOs())).GetRulesResult().Should().BeFalse();
    }

    [Fact(DisplayName = "A Linux rule matches on a Linux host")]
    public void LinuxRule_MatchesOnLinuxHost()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: assumes a Linux CI host.");

        Meta(new Rule("allow", new RuleOs("linux"))).GetRulesResult().Should().BeTrue();
    }

    [Fact(DisplayName = "A Windows rule is skipped on a Linux host")]
    public void WindowsRule_IsSkipped_OnLinuxHost()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: assumes a Linux CI host.");

        var meta = Meta(
            new Rule("allow", new RuleOs()),
            new Rule("disallow", new RuleOs("windows")));

        meta.GetRulesResult().Should().BeTrue();
    }

    [Fact(DisplayName = "An osx rule only matches macOS")]
    public void OsxRule_StartsWithMatches_MacosOnly()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() != EOperatingSystem.MACOS,
            "Host-pinned: asserts a non-macOS host.");

        Meta(new Rule("allow", new RuleOs("osx"))).GetRulesResult().Should().BeFalse();
    }

    [Fact(DisplayName = "An x86 rule matches when the host is 32-bit")]
    public void X86Rule_Matches_WhenHostIs32Bit()
    {
        Assert.SkipUnless(!OSHelper.Is64BitOperatingSystem(), "Host-pinned: requires a 32-bit host.");

        Meta(new Rule("allow", new RuleOs("x86"))).GetRulesResult().Should().BeTrue();
    }

    [Fact(DisplayName = "An x86 rule is skipped when the host is 64-bit")]
    public void X86Rule_IsSkipped_WhenHostIs64Bit()
    {
        Assert.SkipUnless(OSHelper.Is64BitOperatingSystem(), "Host-pinned: requires a 64-bit host.");

        Meta(new Rule("allow", new RuleOs("x86"))).GetRulesResult().Should().BeFalse();
    }

    [Fact(DisplayName = "The last matching rule wins, not the first")]
    public void LastMatchingRule_Wins_NotFirst()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: assumes a Linux CI host.");

        var meta = Meta(
            new Rule("allow", new RuleOs("linux")),
            new Rule("disallow", new RuleOs("linux")));

        meta.GetRulesResult().Should().BeFalse();
    }

    [Fact(DisplayName = "Disallowed by default when no rule matches")]
    public void DisallowedByDefault_WhenNoRuleMatches()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: assumes a Linux CI host.");

        var meta = Meta(
            new Rule("allow", new RuleOs("windows")),
            new Rule("allow", new RuleOs("osx")));

        meta.GetRulesResult().Should().BeFalse();
    }
}
