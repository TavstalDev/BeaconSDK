using Tavstal.BeaconSDK.Helpers.Domain;

namespace Tavstal.BeaconSDK.Tests.Helpers.Domain;

public class GameHelperTests
{
    [Theory(DisplayName = "isNewer should return true when version A is newer than B")]
    [InlineData("1.0.0", "0.0.0")]
    [InlineData("5.0.0", "2.0.0")]
    [InlineData("3.0.0", "1.0.0")]
    public void IsNewer_ReturnsTrue_WhenMajorVersionIsGreater(string versionA, string versionB)
    {
        bool result = GameHelper.isNewer(versionA, versionB);
        Assert.True(result);
    }

    [Theory(DisplayName = "isNewer should return false when version A equals B")]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("1.1.0", "1.1.0")]
    [InlineData("1.1.2", "1.1.2")]
    public void IsNewer_ReturnsFalse_WhenVersionsAreEqual(string versionA, string versionB)
    {
        bool result = GameHelper.isNewer(versionA, versionB);
        Assert.False(result);
    }

    [Theory(DisplayName = "isNewer should treat missing segments as zero (A vs B)")]
    [InlineData("1.0.0", "1.0")]
    [InlineData("1.1.0", "1.1")]
    [InlineData("2.0.0", "2.0")]
    public void IsNewer_TreatsMissingSegmentsAsZero(string versionA, string versionB)
    {
        bool result = GameHelper.isNewer(versionA, versionB);
        Assert.False(result);
    }

    [Theory(DisplayName = "ParseVersionString('A') should parse into B segments")]
    [InlineData("1.0.0", 3)]
    [InlineData("1.1", 2)]
    [InlineData("1.2.3.4", 4)]
    [InlineData("1", 1)]
    public void ParseVersionString_SplitsOnDot_ParsesNumbers(string version, int count)
    {
        var result = GameHelper.ParseVersionString(version);
        Assert.Equal(count, result.Length);
    }

    [Theory(DisplayName = "GetOfflinePlayerUUID('A') should return UUID B")]
    [InlineData("PlayerA", "19CD25E7-F2A9-37B4-A3D6-8FFFDB8AA5D1")]
    [InlineData("PlayerB", "FD33CED3-8AA5-3C17-A85E-E8245423DEFE")]
    [InlineData("PlayerC", "90D6B91E-7ED3-3564-ADC4-7E79C89ABFD2")]
    public void GetOfflinePlayerUUID_ReturnsKnownValue_ForKnownUsername(string username, string uuid)
    {
        var result = GameHelper.GetOfflinePlayerUUID(username);
        Assert.Equal(uuid, result);
    }
}
