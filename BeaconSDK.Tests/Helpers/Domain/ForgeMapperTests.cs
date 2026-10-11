using FluentAssertions;
using Tavstal.BeaconSDK.Helpers.Domain;

namespace Tavstal.BeaconSDK.Tests.Helpers.Domain;

public class ForgeMapperTests
{
    [Fact(DisplayName = "Interpolation replaces braced and unbraced placeholders")]
    public void Interpolation_ReplacesBracedAndUnbracedPlaceholders()
    {
        var values = new Dictionary<string, string?>
        {
            ["name"] = "Forge",
            ["version"] = "1.0"
        };

        var result = ForgeMapper.Interpolation("{name} ${version}", values, false);

        result.Should().Be("Forge 1.0");
    }

    [Fact(DisplayName = "Interpolation leaves unknown placeholders intact")]
    public void Interpolation_LeavesUnknownPlaceholder_Intact()
    {
        var values = new Dictionary<string, string?>();

        var result = ForgeMapper.Interpolation("prefix-{missing}-suffix", values, false);

        result.Should().Be("prefix-{missing}-suffix");
    }

    [Fact(DisplayName = "Interpolation emits an empty string for a null value")]
    public void Interpolation_NullValue_EmitsEmptyString()
    {
        var values = new Dictionary<string, string?> { ["k"] = null };

        var result = ForgeMapper.Interpolation("a{k}b", values, false);

        result.Should().Be("ab");
    }

    [Fact(DisplayName = "HandleEmptyArg quotes a value containing spaces")]
    public void HandleEmptyArg_QuotesValueContainingSpaces()
    {
        ForgeMapper.HandleEmptyArg("k=v with space").Should().Be("k=\"v with space\"");
    }

    [Fact(DisplayName = "HandleEmptyArg quotes a bare argument containing spaces")]
    public void HandleEmptyArg_QuotesBareArgument_WithSpaces()
    {
        ForgeMapper.HandleEmptyArg("hello world").Should().Be("\"hello world\"");
    }

    [Fact(DisplayName = "HandleEmptyArg truncates a value containing equals")]
    public void HandleEmptyArg_TruncatesValueContainingEquals()
    {
        ForgeMapper.HandleEmptyArg("a=b c=d").Should().Be("a=\"b c\"");
    }

    [Fact(DisplayName = "ToFullPath expands group and @extension syntax")]
    public void ToFullPath_ExpandsGroupAtExtensionSyntax()
    {
        var prePath = Path.Combine("root", "libraries");

        var result = ForgeMapper.ToFullPath("[net.minecraftforge:forge:1.0.0@jar]", prePath);

        var expected = Path.Combine(prePath, "net", "minecraftforge", "forge", "1.0.0", "forge-1.0.0.jar");
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "ToFullPath strips surrounding quotes")]
    public void ToFullPath_StripsSurroundingQuotes()
    {
        ForgeMapper.ToFullPath("'quoted value'", "").Should().Be("quoted value");
    }

    [Fact(DisplayName = "Map skips path conversion with an empty pre-path")]
    public void Map_WithEmptyPrePath_SkipsPathConversion()
    {
        var args = new[] { "[net.minecraftforge:forge:1.0.0@jar]" };

        var result = ForgeMapper.Map(args, new Dictionary<string, string?>(), "");

        result.Should().ContainSingle().Which.Should().Be("[net.minecraftforge:forge:1.0.0@jar]");
    }

    [Fact(DisplayName = "Map skips HandleEmptyArg when the argument is already quoted")]
    public void Map_SkipsHandleEmptyArg_WhenAlreadyQuoted()
    {
        var args = new[] { "\"already quoted\"" };

        var result = ForgeMapper.Map(args, new Dictionary<string, string?>(), "");

        result.Should().ContainSingle().Which.Should().Be("\"already quoted\"");
    }
}
