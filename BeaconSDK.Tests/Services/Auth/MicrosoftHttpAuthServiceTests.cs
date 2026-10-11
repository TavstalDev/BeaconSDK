using System.Net;
using System.Net.Sockets;
using System.Reflection;
using FluentAssertions;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Services.Implementations.Auth;
using Tavstal.BeaconSDK.Tests.Fakes;

namespace Tavstal.BeaconSDK.Tests.Services.Auth;

public class MicrosoftHttpAuthServiceTests
{
    private static readonly FieldInfo ListenerField =
        typeof(MicrosoftHttpAuthService).GetField("_httpListener", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo ListeningField =
        typeof(MicrosoftHttpAuthService).GetField("_isListening", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static HttpListener CreateStartedListener()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{GetFreePort()}/");
        listener.Start();
        return listener;
    }

    [Fact(DisplayName = "Success status change automatically stops the listener")]
    public async Task OnAuthStatusChanged_Success_AutoStopsListener()
    {
        var fake = new FakeMicrosoftAuthService();
        var service = new MicrosoftHttpAuthService(
            new CapturingBeaconLogger<MicrosoftHttpAuthService>(), fake);

        var listener = CreateStartedListener();
        ListenerField.SetValue(service, listener);
        ListeningField.SetValue(service, true);

        fake.RaiseStatus(EAuthStatus.SUCCESS);

        var listening = true;
        for (var i = 0; i < 100 && listening; i++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
            listening = (bool)ListeningField.GetValue(service)!;
        }

        listening.Should().BeFalse();
        listener.IsListening.Should().BeFalse();
    }

    [Fact(DisplayName = "StartListening then Stop cleans up the listener")]
    public async Task StartListening_ThenStop_CleansUpListener()
    {
        var fake = new FakeMicrosoftAuthService();
        var service = new MicrosoftHttpAuthService(
            new CapturingBeaconLogger<MicrosoftHttpAuthService>(), fake);

        var listener = CreateStartedListener();
        ListenerField.SetValue(service, listener);
        ListeningField.SetValue(service, true);

        await service.StopListeningAsync(true, TestContext.Current.CancellationToken);

        ((bool)ListeningField.GetValue(service)!).Should().BeFalse();
        listener.IsListening.Should().BeFalse();
    }
}
