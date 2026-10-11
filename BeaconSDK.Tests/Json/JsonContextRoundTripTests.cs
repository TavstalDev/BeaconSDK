using System.Text.Json;
using FluentAssertions;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Models.Accounts;
using Tavstal.BeaconSDK.Models.Json;
using Tavstal.BeaconSDK.Models.Microsoft;
using Tavstal.BeaconSDK.Models.MojangApi;
using Tavstal.BeaconSDK.Models.MojangApi.Meta.Library;

namespace Tavstal.BeaconSDK.Tests.Json;

public class JsonContextRoundTripTests
{
    [Fact(DisplayName = "Account round-trips via BeaconJsonContext")]
    public void Account_RoundTrips_ViaBeaconJsonContext()
    {
        var account = new Account
        {
            Id = "id-1",
            Uuid = "uuid-1",
            DisplayName = "RoundTripper",
            Type = EAccountType.MICROSOFT
        };

        var json = JsonSerializer.Serialize(account, BeaconJsonContext.Default.Account);
        var restored = JsonSerializer.Deserialize(json, BeaconJsonContext.Default.Account);

        restored.Should().NotBeNull();
        restored.Id.Should().Be("id-1");
        restored.DisplayName.Should().Be("RoundTripper");
        restored.Type.Should().Be(EAccountType.MICROSOFT);
    }

    [Fact(DisplayName = "VersionManifest round-trips via CoreJsonContext")]
    public void VersionManifest_RoundTrips_ViaCoreJsonContext()
    {
        var manifest = new VersionManifest(
            new VersionManifestLatest { Release = "1.20.4", Snapshot = "24w01a" },
            [
                new MinecraftVersion("1.20.4", "release", "https://example.com/1.20.4.json",
                    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            ]);

        var json = JsonSerializer.Serialize(manifest, CoreJsonContext.Default.VersionManifest);
        var restored = JsonSerializer.Deserialize(json, CoreJsonContext.Default.VersionManifest);

        restored.Should().NotBeNull();
        restored.Latest.Release.Should().Be("1.20.4");
        restored.Latest.Snapshot.Should().Be("24w01a");
        restored.Versions.Should().ContainSingle().Which.Id.Should().Be("1.20.4");
    }

    [Fact(DisplayName = "List<Rule> round-trips via CoreJsonContext")]
    public void ListRule_RoundTrips_ViaCoreJsonContext()
    {
        var rules = new List<Rule>
        {
            new("allow", new RuleOs("linux")),
            new("disallow", new RuleOs("windows"))
        };

        var json = JsonSerializer.Serialize(rules, CoreJsonContext.Default.ListRule);
        var restored = JsonSerializer.Deserialize(json, CoreJsonContext.Default.ListRule);

        restored.Should().NotBeNull().And.HaveCount(2);
        restored[0].Action.Should().Be("allow");
        restored[0].Os!.Name.Should().Be("linux");
        restored[1].Action.Should().Be("disallow");
        restored[1].Os!.Name.Should().Be("windows");
    }

    [Fact(DisplayName = "DeviceCodeResult round-trips")]
    public void DeviceCodeResult_RoundTrips()
    {
        var result = new DeviceCodeResult("device", "user", "https://example.com/verify", 900, 5);

        var json = JsonSerializer.Serialize(result, CoreJsonContext.Default.DeviceCodeResult);
        var restored = JsonSerializer.Deserialize(json, CoreJsonContext.Default.DeviceCodeResult);

        restored.Should().NotBeNull();
        restored.Should().BeEquivalentTo(result);
    }

    [Fact(DisplayName = "XboxTokenRequestBody serializes with expected property names")]
    public void XboxRequestBody_Serializes_WithExpectedPropertyNames()
    {
        var request = new XboxTokenRequestBody
        {
            TokenType = "JWT",
            RelyingParty = "http://auth.xboxlive.com",
            Properties = new XboxTokenProperties
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = "ticket-value"
            }
        };

        var json = JsonSerializer.Serialize(request, CoreJsonContext.Default.XboxTokenRequestBody);

        json.Should().Contain("TokenType")
            .And.Contain("RelyingParty")
            .And.Contain("Properties")
            .And.Contain("AuthMethod")
            .And.Contain("SiteName")
            .And.Contain("RpsTicket");
    }

    [Fact(DisplayName = "Dictionary<string, string> round-trips via GenericJsonContext")]
    public void DictionaryStringString_RoundTrips_ViaGenericJsonContext()
    {
        var dictionary = new Dictionary<string, string>
        {
            ["alpha"] = "one",
            ["beta"] = "two"
        };

        var json = JsonSerializer.Serialize(dictionary, GenericJsonContext.Default.DictionaryStringString);
        var restored = JsonSerializer.Deserialize(json, GenericJsonContext.Default.DictionaryStringString);

        restored.Should().NotBeNull().And.BeEquivalentTo(dictionary);
    }
}
