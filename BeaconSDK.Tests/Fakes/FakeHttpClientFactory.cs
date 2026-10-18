namespace Tavstal.BeaconSDK.Tests.Fakes;

/// <summary>
///     An <see cref="IHttpClientFactory" /> that hands out <see cref="HttpClient" /> instances backed by a shared
///     <see cref="StubHttpMessageHandler" />, standing in for the SDK's named "default" client.
/// </summary>
public sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly StubHttpMessageHandler _handler;

    public FakeHttpClientFactory(StubHttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_handler, false);
    }
}
