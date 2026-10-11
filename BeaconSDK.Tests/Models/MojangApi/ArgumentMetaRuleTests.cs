using System.Text.Json;
using FluentAssertions;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Models.Json;
using Tavstal.BeaconSDK.Models.MojangApi.Meta;

namespace Tavstal.BeaconSDK.Tests.Models.MojangApi;

public class ArgumentMetaRuleTests
{
    private static ArgumentMeta Meta()
    {
        return new ArgumentMeta();
    }

    [Fact(DisplayName = "GetRuleResult returns non-JSON input verbatim")]
    public void GetRuleResult_NonJsonInput_ReturnsInputVerbatim()
    {
        Meta().GetRuleResult("plain-value").Should().Be("plain-value");
    }

    [Fact(DisplayName = "GetRuleResult returns null for invalid JSON")]
    public void GetRuleResult_InvalidJson_ReturnsNull()
    {
        Meta().GetRuleResult("{ this is not valid json }").Should().BeNull();
    }

    [Fact(DisplayName = "GetRuleResult returns the joined value when rules are empty")]
    public void GetRuleResult_EmptyRules_ReturnsJoinedValue()
    {
        var json = "{\"value\":\"the-value\",\"rules\":[{\"action\":\"allow\",\"os\":{\"name\":\"\"}}]}";

        Meta().GetRuleResult(json).Should().Be("the-value");
    }

    [Fact(DisplayName = "GetRuleResult joins array values with spaces")]
    public void GetRuleResult_ArrayValue_JoinedWithSpaces()
    {
        var json = "{\"value\":[\"a\",\"b\",\"c\"],\"rules\":[{\"action\":\"allow\"}]}";

        Meta().GetRuleResult(json).Should().Be("a b c");
    }

    [Fact(DisplayName = "GetRuleResult returns a string value as-is")]
    public void GetRuleResult_StringValue_ReturnedAsIs()
    {
        var json = "{\"value\":\"-Dfoo=bar\",\"rules\":[{\"action\":\"allow\"}]}";

        Meta().GetRuleResult(json).Should().Be("-Dfoo=bar");
    }

    [Fact(DisplayName = "GetRuleResult returns null for a disallow rule")]
    public void GetRuleResult_DisallowRule_ReturnsNull()
    {
        var json = "{\"value\":\"secret\",\"rules\":[{\"action\":\"disallow\"}]}";

        Meta().GetRuleResult(json).Should().BeNull();
    }

    [Fact(DisplayName = "GetRuleResult deserializes rules via CoreJsonContext List<Rule>")]
    public void GetRuleResult_RulesDeserializedViaCoreJsonContext_ListRule()
    {
        Assert.SkipUnless(OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: assumes a Linux CI host.");

        var json = "{\"value\":\"-Dfoo=bar\",\"rules\":[{\"action\":\"allow\",\"os\":{\"name\":\"linux\"}}]}";

        Meta().GetRuleResult(json).Should().Be("-Dfoo=bar");

        var listRuleJson = "[{\"action\":\"allow\",\"os\":{\"name\":\"linux\"}}]";
        var rules = JsonSerializer.Deserialize(listRuleJson, CoreJsonContext.Default.ListRule);

        rules.Should().NotBeNull().And.ContainSingle();
        rules[0].Action.Should().Be("allow");
        rules[0].Os!.Name.Should().Be("linux");
    }
}
