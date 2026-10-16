using System.Text.Json;
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

        Assert.NotNull(restored);
        Assert.Equal("id-1", restored.Id);
        Assert.Equal("RoundTripper", restored.DisplayName);
        Assert.Equal(EAccountType.MICROSOFT, restored.Type);
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

        Assert.NotNull(restored);
        Assert.Equal("1.20.4", restored.Latest.Release);
        Assert.Equal("24w01a", restored.Latest.Snapshot);
        Assert.Equal("1.20.4", Assert.Single(restored.Versions).Id);
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

        Assert.NotNull(restored);
        Assert.Equal(2, restored.Count);
        Assert.Equal("allow", restored[0].Action);
        Assert.Equal("linux", restored[0].Os!.Name);
        Assert.Equal("disallow", restored[1].Action);
        Assert.Equal("windows", restored[1].Os!.Name);
    }

    [Fact(DisplayName = "DeviceCodeResult round-trips")]
    public void DeviceCodeResult_RoundTrips()
    {
        var result = new DeviceCodeResult("device", "user", "https://example.com/verify", 900, 5);

        var json = JsonSerializer.Serialize(result, CoreJsonContext.Default.DeviceCodeResult);
        var restored = JsonSerializer.Deserialize(json, CoreJsonContext.Default.DeviceCodeResult);

        Assert.NotNull(restored);
        Assert.Equal(result.DeviceCode, restored.DeviceCode);
        Assert.Equal(result.UserCode, restored.UserCode);
        Assert.Equal(result.VerificationUri, restored.VerificationUri);
        Assert.Equal(result.ExpiresIn, restored.ExpiresIn);
        Assert.Equal(result.Interval, restored.Interval);
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

        Assert.Contains("TokenType", json);
        Assert.Contains("RelyingParty", json);
        Assert.Contains("Properties", json);
        Assert.Contains("AuthMethod", json);
        Assert.Contains("SiteName", json);
        Assert.Contains("RpsTicket", json);
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

        Assert.NotNull(restored);
        Assert.Equal(dictionary.Count, restored.Count);
        foreach (var pair in dictionary)
            Assert.Equal(pair.Value, restored[pair.Key]);
    }
}
