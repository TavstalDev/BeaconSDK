using System.Text.Json;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Models.Accounts;
using Tavstal.BeaconSDK.Services.Implementations;
using Tavstal.BeaconSDK.Tests.Fakes;
using Tavstal.BeaconSDK.Tests.Fixtures;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Tavstal.BeaconSDK.Tests.Models.Accounts;

public class AccountTests
{
    [Fact(DisplayName = "Set then get access token round-trips through the recording encryption service")]
    public void SetThenGetAccessToken_RoundTrips_ThroughRecordingEncryptionService()
    {
        var encryption = new RecordingEncryptionService();
        var account = new Account();
        var plain = "access-token-value"u8.ToArray();

        account.SetAccessToken(plain, encryption);

        Assert.Equal(plain, account.GetAccessToken(encryption));
    }

    [Fact(DisplayName = "SetAccessToken uses Reprotect rather than Encrypt")]
    public void SetAccessToken_UsesReprotect_NotEncrypt()
    {
        var encryption = new RecordingEncryptionService();
        var account = new Account();

        account.SetAccessToken("token"u8.ToArray(), encryption);

        Assert.Equal(1, encryption.ReprotectCallCount);
        Assert.Equal(0, encryption.EncryptCallCount);
    }

    [Fact(DisplayName = "SetAccessToken leaves plaintext input unchanged when Reprotect fails")]
    public void SetAccessToken_WithPlainTextInput_LeavesInputUnchanged_WhenReprotectFails()
    {
        Assert.SkipUnless(
            Tavstal.BeaconSDK.Helpers.Platform.OSHelper.GetOperatingSystem() == EOperatingSystem.LINUX,
            "Host-pinned: the no-provider encryption fallback only works on Linux/macOS hosts.");

        var encryption = new EncryptionService(new CapturingBeaconLogger<EncryptionService>(), ModelFactory.CreateAppConfiguration());
        var plain = "plaintext-token-value"u8.ToArray();
        var account = new Account();

        account.SetAccessToken(plain, encryption);

        Assert.Equal(plain, account.EncryptedAccessToken);
    }

    [Fact(DisplayName = "Token arrays are initialized to empty by the constructor")]
    public void TokenArrays_CtorInitializedToEmpty()
    {
        var account = new Account();

        Assert.Empty(account.EncryptedAccessToken);
        Assert.Empty(account.EncryptedRefreshToken);
    }

    [Fact(DisplayName = "CanExpire is false for offline and true for Microsoft accounts")]
    public void CanExpire_FalseForOffline_TrueForMicrosoft()
    {
        Assert.False(new Account { Type = EAccountType.OFFLINE }.CanExpire);
        Assert.True(new Account { Type = EAccountType.MICROSOFT }.CanExpire);
    }

    [Fact(DisplayName = "JSON round-trip via BeaconJsonContext preserves tokens and enum")]
    public void JsonRoundTrip_ViaBeaconJsonContext_PreservesTokensAndEnum()
    {
        var account = new Account
        {
            Id = "account-id",
            Uuid = "11111111-1111-1111-1111-111111111111",
            DisplayName = "PlayerOne",
            Type = EAccountType.MICROSOFT,
            EncryptedAccessToken = [1, 2, 3],
            EncryptedRefreshToken = [4, 5],
            AccessTokenExpireDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(account, BeaconJsonContext.Default.Account);
        var restored = JsonSerializer.Deserialize(json, BeaconJsonContext.Default.Account);

        Assert.NotNull(restored);
        Assert.Equal(account.Id, restored.Id);
        Assert.Equal(account.Uuid, restored.Uuid);
        Assert.Equal(account.DisplayName, restored.DisplayName);
        Assert.Equal(account.Type, restored.Type);
        Assert.Equal(account.EncryptedAccessToken, restored.EncryptedAccessToken);
        Assert.Equal(account.EncryptedRefreshToken, restored.EncryptedRefreshToken);
        Assert.Equal(account.AccessTokenExpireDate, restored.AccessTokenExpireDate);
        Assert.Equal(account.Skins, restored.Skins);
        Assert.Equal(account.MojangProfile, restored.MojangProfile);
        Assert.Equal(EAccountType.MICROSOFT, restored.Type);
    }
}
