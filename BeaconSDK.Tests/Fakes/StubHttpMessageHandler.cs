using System.Net;
using System.Text;

namespace Tavstal.BeaconSDK.Tests.Fakes;

/// <summary>
///     A canned-response <see cref="HttpMessageHandler" />. Responses are dequeued in the
///     order they were enqueued; every request is captured for later assertions.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responders = new();

    public List<CapturedRequest> Requests { get; } = new();

    public StubHttpMessageHandler Enqueue(HttpStatusCode statusCode, string content,
        string contentType = "application/json")
    {
        return Enqueue(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, contentType)
        });
    }

    public StubHttpMessageHandler Enqueue(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responders.Enqueue(responder);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? body = null;
        if (request.Content != null)
            body = await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add(new CapturedRequest(request.Method.Method, request.RequestUri?.ToString() ?? string.Empty,
            body));

        if (_responders.Count == 0)
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

        return _responders.Dequeue()(request);
    }

    public sealed record CapturedRequest(string Method, string Url, string? Body);
}
