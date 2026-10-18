using System.Net;
using System.Text;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;
using Tavstal.BeaconSDK.Services.Abstractions.Auth;

namespace Tavstal.BeaconSDK.Services.Implementations.Auth;

/// <inheritdoc />
public class MicrosoftHttpAuthService : IMicrosoftHttpAuthService
{
    /// <summary>
    ///     The loopback URL the service listens on to receive the Microsoft authentication redirect.
    /// </summary>
    public const string ListeningUrl = "http://localhost:43319/";

    private readonly IBeaconLogger _logger;
    private readonly IMicrosoftAuthService _microsoftAuthService;
    private HttpListener? _httpListener;
    private bool _isListening;
    private IProgressReporter? _progressReporter;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MicrosoftHttpAuthService" /> class and subscribes
    ///     to authentication status changes so the local listener can be stopped after successful sign-in.
    /// </summary>
    /// <param name="logger">Logger used for diagnostic and error output.</param>
    /// <param name="microsoftAuthService">
    ///     The Microsoft authentication service that raises authentication status change notifications.
    /// </param>
    public MicrosoftHttpAuthService(IBeaconLogger<MicrosoftHttpAuthService> logger,
        IMicrosoftAuthService microsoftAuthService)
    {
        _logger = logger;
        _microsoftAuthService = microsoftAuthService;
        _microsoftAuthService.OnAuthStatusChanged += OnAuthStatusChanged;
    }

    /// <inheritdoc />
    public async Task StartListeningAsync(IProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        _progressReporter = progressReporter;

        if (_httpListener == null)
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(ListeningUrl);
        }

        if (_isListening)
            return;

        try
        {
            _progressReporter?.Report(EProgressKind.AUTH_LISTENER_STARTING, 0);
            _httpListener.Start();
            _isListening = true;
        }
        catch (HttpListenerException ex)
        {
            _progressReporter?.Report(EProgressKind.AUTH_LISTENER_FAILED, 0);
            _logger.LogError(ex, "Failed to start HTTP listener:");
            _isListening = false;
            return;
        }

        while (_isListening)
        {
            var context = await _httpListener.GetContextAsync(); // get te context
            await HandleHttpRequestAsync(context, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopListeningAsync(bool cancelled = true, CancellationToken cancellationToken = default)
    {
        if (_httpListener == null)
            return Task.CompletedTask;

        if (!_isListening)
            return Task.CompletedTask;

        if (cancelled)
            _progressReporter?.Report(EProgressKind.AUTH_LISTENER_CANCELLED, 0);
        _isListening = false;
        _httpListener.Stop();
        _httpListener.Close();
        _httpListener = null;
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles authentication status updates from the Microsoft authentication service.
    ///     Stops the local HTTP listener once authentication succeeds.
    /// </summary>
    /// <param name="status">The current authentication status.</param>
    private void OnAuthStatusChanged(EAuthStatus status)
    {
        if (status == EAuthStatus.SUCCESS)
            Task.Run(async () => await StopListeningAsync());
    }

    /// <summary>
    ///     Handles an incoming HTTP request from the local authentication listener.
    ///     Routes the request to the appropriate handler based on the URL path.
    /// </summary>
    /// <param name="context">The HTTP context containing the request and response objects.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleHttpRequestAsync(HttpListenerContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Request.RawUrl == null)
            return;

        _logger.LogDebug("Received HTTP request: " + context.Request.RawUrl);
        if (context.Request.QueryString.AllKeys.Any(x => x == "LogError"))
        {
            _logger.LogError("Authentication LogError received in callback.");
            _logger.LogError(context.Request.QueryString.Get("LogError_description") ?? "Unknown LogError");
            return;
        }

        if (context.Request.RawUrl.StartsWith("/microsoft/authcallback"))
        {
            await CloseBrowserAsync(context, cancellationToken);
            _progressReporter?.Report(EProgressKind.AUTH_LISTENER_CALLBACK, 0);
            await _microsoftAuthService.HandleHttpRequestAsync(context.Request, _progressReporter, cancellationToken);
            return;
        }

        if (context.Request.RawUrl.StartsWith("/cancel"))
        {
            await CloseBrowserAsync(context, cancellationToken);
            return;
        }

        // Send Browser response
        await CloseBrowserAsync(context, cancellationToken);
    }

    /// <summary>
    ///     Sends an HTML response to the client that closes the browser window.
    /// </summary>
    /// <param name="context">The HTTP context containing the response object to write to.</param>
    /// <param name="cancellationToken">Token to cancel the write operation if needed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CloseBrowserAsync(HttpListenerContext context, CancellationToken cancellationToken = default)
    {
        const string responseString = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Microsoft Authentication</title>
                </head>
                <body>
                    <h1>You may close this window.</h1>
                </body>
                <script>
                    window.open('', '_self').close();
                </script>
                </html>";
        var buffer = Encoding.UTF8.GetBytes(responseString);

        var response = context.Response;
        response.ContentType = "text/html";
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        response.Close();
    }
}
