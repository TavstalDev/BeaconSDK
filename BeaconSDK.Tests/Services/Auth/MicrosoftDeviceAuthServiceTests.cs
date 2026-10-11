using FluentAssertions;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Services.Implementations.Auth;
using Tavstal.BeaconSDK.Tests.Fakes;

namespace Tavstal.BeaconSDK.Tests.Services.Auth;

public class MicrosoftDeviceAuthServiceTests
{
    private static readonly TimeSpan FastInterval = TimeSpan.FromMilliseconds(10);

    [Fact(DisplayName = "StartListening polls until the auth status leaves pending")]
    public async Task StartListening_PollsUntilAuthStatusLeavesPending()
    {
        var fake = new FakeMicrosoftAuthService
        {
            PollsBeforeStatusChange = 2,
            StatusAfterPolls = EAuthStatus.SUCCESS
        };
        var service = new MicrosoftDeviceAuthService(fake);

        await service.StartListeningAsync("device-code", FastInterval, TestContext.Current.CancellationToken);

        fake.CheckDeviceCodeCallCount.Should().Be(2);
        fake.AuthStatus.Should().Be(EAuthStatus.SUCCESS);
    }

    [Fact(DisplayName = "StartListening stops after success")]
    public async Task StartListening_StopsAfterSuccess()
    {
        var fake = new FakeMicrosoftAuthService
        {
            PollsBeforeStatusChange = 1,
            StatusAfterPolls = EAuthStatus.SUCCESS
        };
        var service = new MicrosoftDeviceAuthService(fake);

        await service.StartListeningAsync("device-code", FastInterval, TestContext.Current.CancellationToken);

        fake.CheckDeviceCodeCallCount.Should().Be(1);
        fake.AuthStatus.Should().Be(EAuthStatus.SUCCESS);
    }

    [Fact(DisplayName = "StartListening stops on failed status")]
    public async Task StartListening_StopsOnFailed()
    {
        var fake = new FakeMicrosoftAuthService
        {
            PollsBeforeStatusChange = 1,
            StatusAfterPolls = EAuthStatus.FAILED
        };
        var service = new MicrosoftDeviceAuthService(fake);

        await service.StartListeningAsync("device-code", FastInterval, TestContext.Current.CancellationToken);

        fake.CheckDeviceCodeCallCount.Should().Be(1);
        fake.AuthStatus.Should().Be(EAuthStatus.FAILED);
    }

    [Fact(DisplayName = "StartListening respects the cancellation token")]
    public async Task StartListening_RespectsCancellationToken()
    {
        var fake = new FakeMicrosoftAuthService
        {
            PollsBeforeStatusChange = 10_000,
            StatusAfterPolls = EAuthStatus.SUCCESS
        };
        var service = new MicrosoftDeviceAuthService(fake);
        using var cts = new CancellationTokenSource();

        var task = service.StartListeningAsync("device-code", TimeSpan.FromSeconds(5), cts.Token);

        while (fake.CheckDeviceCodeCallCount < 1)
            await Task.Delay(5, cts.Token);

        await cts.CancelAsync();

        var act = async () => await task;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact(DisplayName = "A second StartListening call while running is a no-op")]
    public async Task StartListening_SecondCallWhileRunning_IsNoOp()
    {
        var fake = new FakeMicrosoftAuthService
        {
            PollsBeforeStatusChange = 3,
            StatusAfterPolls = EAuthStatus.FAILED
        };
        var service = new MicrosoftDeviceAuthService(fake);

        var first = service.StartListeningAsync("device-code", FastInterval, TestContext.Current.CancellationToken);
        var second = service.StartListeningAsync("device-code", FastInterval, TestContext.Current.CancellationToken);

        await second;
        await first;

        fake.CheckDeviceCodeCallCount.Should().Be(3);
    }

    [Fact(DisplayName = "StopListening prevents further polls")]
    public async Task StopListening_PreventsFurtherPolls()
    {
        var fake = new FakeMicrosoftAuthService
        {
            PollsBeforeStatusChange = 10_000,
            StatusAfterPolls = EAuthStatus.SUCCESS
        };
        var service = new MicrosoftDeviceAuthService(fake);

        var task = service.StartListeningAsync("device-code", FastInterval, TestContext.Current.CancellationToken);

        while (fake.CheckDeviceCodeCallCount < 1)
            await Task.Delay(5, TestContext.Current.CancellationToken);

        await service.StopListeningAsync(TestContext.Current.CancellationToken);
        var countAfterStop = fake.CheckDeviceCodeCallCount;

        await Task.Delay(100, TestContext.Current.CancellationToken);

        fake.CheckDeviceCodeCallCount.Should().Be(countAfterStop);
        await task;
    }
}
