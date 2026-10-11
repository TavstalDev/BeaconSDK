using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Models.Endpoints;
using Tavstal.BeaconSDK.Services.Implementations.Auth;
using Tavstal.BeaconSDK.Tests.Fakes;

namespace Tavstal.BeaconSDK.Tests.Services.Auth;

public class MicrosoftAuthServiceTests
{
    private static string TestData(string fileName)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "TestData", fileName));
    }

    private static (MicrosoftAuthService Service, StubHttpMessageHandler Handler, RecordingEncryptionService Encryption)
        CreateService()
    {
        var handler = new StubHttpMessageHandler();
        var http = new FakeHttpService(handler);
        var encryption = new RecordingEncryptionService();
        var service = new MicrosoftAuthService(new CapturingBeaconLogger<MicrosoftAuthService>(), http, encryption);
        return (service, handler, encryption);
    }

    private static void EnqueueFullSuccessChain(StubHttpMessageHandler handler)
    {
        handler
            .Enqueue(HttpStatusCode.OK, TestData("microsoft_token.json"))
            .Enqueue(HttpStatusCode.OK, TestData("xbox_token.json"))
            .Enqueue(HttpStatusCode.OK, TestData("xbox_xsts.json"))
            .Enqueue(HttpStatusCode.OK, TestData("minecraft_access.json"))
            .Enqueue(HttpStatusCode.OK, TestData("ownership.json"))
            .Enqueue(HttpStatusCode.OK, TestData("minecraft_profile.json"));
    }

    [Fact(DisplayName = "CreateDeviceCodeAsync parses the device code and polling interval")]
    public async Task CreateDeviceCodeAsync_ParsesDeviceCodeAndInterval()
    {
        var (service, handler, _) = CreateService();
        handler.Enqueue(HttpStatusCode.OK, TestData("device_code.json"));

        var result = await service.CreateDeviceCodeAsync(cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.DeviceCode.Should().Be("device-code-123");
        result.Interval.Should().Be(5);
        result.ExpiresIn.Should().Be(900);
    }

    [Fact(DisplayName = "Pending device code transitions to processing and raises OnAuthStatusChanged")]
    public async Task CheckDeviceCodeAsync_Pending_TransitionsToProcessing_AndRaisesOnAuthStatusChanged()
    {
        var (service, handler, _) = CreateService();
        EnqueueFullSuccessChain(handler);

        var statuses = new List<EAuthStatus>();
        service.OnAuthStatusChanged += statuses.Add;

        await service.CheckDeviceCodeAsync("device-code-123", TestContext.Current.CancellationToken);

        statuses.Should().ContainInOrder(EAuthStatus.PROCESSING, EAuthStatus.SUCCESS);
    }

    [Fact(DisplayName = "Accepted device code calls the Xbox token endpoint")]
    public async Task CheckDeviceCodeAsync_Accepted_CallsXboxTokenEndpoint()
    {
        var (service, handler, _) = CreateService();
        EnqueueFullSuccessChain(handler);

        await service.CheckDeviceCodeAsync("device-code-123", TestContext.Current.CancellationToken);

        service.AuthStatus.Should().Be(EAuthStatus.SUCCESS);
        handler.Requests.Should().Contain(r => r.Url == MicrosoftEndpoints.XboxAuthUrl);
    }

    [Fact(DisplayName = "XboxTokenCallAsync parses the token and raises status")]
    public async Task XboxTokenCallAsync_ParsesToken_AndRaisesStatus()
    {
        var (service, handler, _) = CreateService();
        handler
            .Enqueue(HttpStatusCode.OK, TestData("xbox_token.json"))
            .Enqueue(HttpStatusCode.OK, TestData("xbox_xsts.json"))
            .Enqueue(HttpStatusCode.OK, TestData("minecraft_access.json"))
            .Enqueue(HttpStatusCode.OK, TestData("ownership.json"))
            .Enqueue(HttpStatusCode.OK, TestData("minecraft_profile.json"));

        await service.XboxTokenCallAsync("token", "refresh", TestContext.Current.CancellationToken);

        service.AuthStatus.Should().Be(EAuthStatus.SUCCESS);
        service.Account.Should().NotBeNull();
    }

    [Fact(DisplayName = "MinecraftAccessCallAsync builds the identity token in XBL3.0 format")]
    public async Task MinecraftAccessCallAsync_BuildsIdentityToken_Xbl3Format()
    {
        var (service, handler, _) = CreateService();
        handler
            .Enqueue(HttpStatusCode.OK, TestData("minecraft_access.json"))
            .Enqueue(HttpStatusCode.OK, TestData("ownership.json"))
            .Enqueue(HttpStatusCode.OK, TestData("minecraft_profile.json"));

        await service.MinecraftAccessCallAsync("xsts-token", "refresh", "uhs-value",
            TestContext.Current.CancellationToken);

        var request = handler.Requests.Single(r => r.Url == MicrosoftEndpoints.MinecraftAuthUrl);
        request.Body.Should().NotBeNull();
        using var document = JsonDocument.Parse(request.Body!);
        document.RootElement.GetProperty("identityToken").GetString()
            .Should().Be("XBL3.0 x=uhs-value;xsts-token");
        document.RootElement.GetProperty("ensureLegacyEnabled").GetBoolean().Should().BeTrue();
    }

    [Theory(DisplayName = "Ownership detection accepts game/product Minecraft and Bedrock products")]
    [InlineData("game_minecraft")]
    [InlineData("game_minecraft_bedrock")]
    [InlineData("product_minecraft")]
    [InlineData("product_minecraft_bedrock")]
    public async Task CheckMinecraftOwnershipAsync_DetectsGameMinecraft_ThreeGameProducts(string productId)
    {
        var (service, handler, _) = CreateService();
        var ownership = $"{{\"keyId\":\"key\",\"signature\":\"sig\",\"items\":[{{\"name\":\"{productId}\",\"signature\":\"sig\"}}]}}";
        handler
            .Enqueue(HttpStatusCode.OK, ownership)
            .Enqueue(HttpStatusCode.OK, TestData("minecraft_profile.json"));

        await service.CheckMinecraftOwnershipAsync("mc-token", "refresh", 3600,
            TestContext.Current.CancellationToken);

        service.AuthStatus.Should().Be(EAuthStatus.SUCCESS);
    }

    [Fact(DisplayName = "GetMinecraftProfileAsync builds the account with skins and active cape")]
    public async Task GetMinecraftProfileAsync_BuildsAccount_SkinsAndActiveCape()
    {
        var (service, handler, encryption) = CreateService();
        handler.Enqueue(HttpStatusCode.OK, TestData("minecraft_profile.json"));

        await service.GetMinecraftProfileAsync("mc-token", "refresh-token", 3600,
            TestContext.Current.CancellationToken);

        service.Account.Should().NotBeNull();
        service.Account!.DisplayName.Should().Be("PlayerOne");
        service.Account.Skins.Should().HaveCount(2);
        service.Account.Skins[0].CapeId.Should().Be("cape-active");
        service.Account.Skins[0].Model.Should().Be("classic");
        Encoding.UTF8.GetString(service.Account.GetAccessToken(encryption)).Should().Be("mc-token");
    }

    [Fact(DisplayName = "RefreshLoginAsync success updates tokens through the encryption service")]
    public async Task RefreshLoginAsync_Success_UpdatesTokens_ThroughEncryptionService()
    {
        var (service, handler, encryption) = CreateService();
        EnqueueFullSuccessChain(handler);

        var result = await service.RefreshLoginAsync("refresh-token", TestContext.Current.CancellationToken);

        result.Should().BeTrue();
        service.AuthStatus.Should().Be(EAuthStatus.SUCCESS);
        encryption.ReprotectCallCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact(DisplayName = "Auth status transitions pending to processing to success in order")]
    public async Task AuthStatus_TransitionsPendingProcessingSuccess_InOrder()
    {
        var (service, handler, _) = CreateService();
        EnqueueFullSuccessChain(handler);

        var statuses = new List<EAuthStatus>();
        service.OnAuthStatusChanged += statuses.Add;

        await service.CheckDeviceCodeAsync("device-code-123", TestContext.Current.CancellationToken);

        statuses.Should().ContainInOrder(EAuthStatus.PROCESSING, EAuthStatus.SUCCESS);
        statuses.Should().NotContain(EAuthStatus.FAILED);
    }

    [Fact(DisplayName = "HTTP failure sets status to failed and raises the event")]
    public async Task HttpFailure_SetsStatusFailed_AndRaisesEvent()
    {
        var (service, handler, _) = CreateService();
        handler.Enqueue(HttpStatusCode.InternalServerError, "{}");

        var statuses = new List<EAuthStatus>();
        service.OnAuthStatusChanged += statuses.Add;

        var result = await service.RefreshLoginAsync("refresh-token", TestContext.Current.CancellationToken);

        result.Should().BeFalse();
        service.AuthStatus.Should().Be(EAuthStatus.FAILED);
        statuses.Should().Contain(EAuthStatus.FAILED);
    }
}
