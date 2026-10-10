using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Services.Abstractions.Auth;

namespace Tavstal.BeaconSDK.Services.Implementations.Auth;

/// <inheritdoc />
public class MicrosoftDeviceAuthService : IMicrosoftDeviceAuthService
{
    private readonly IMicrosoftAuthService _microsoftAuthService;
    private bool _isListening;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MicrosoftDeviceAuthService" /> class.
    /// </summary>
    /// <param name="microsoftAuthService">The Microsoft authentication service to use for checking device codes.</param>
    public MicrosoftDeviceAuthService(IMicrosoftAuthService microsoftAuthService)
    {
        _microsoftAuthService = microsoftAuthService;
    }

    /// <inheritdoc />
    public async Task StartListeningAsync(string deviceCode, TimeSpan interval,
        CancellationToken cancellationToken = default)
    {
        // Prevents starting the listener if it is already active.
        if (_isListening)
            return;

        _isListening = true;

        // Continuously checks the device authentication status while the listener is active.
        while (_isListening)
        {
            // Stops listening if the authentication status is no longer pending or none.
            if (!(_microsoftAuthService.AuthStatus == EAuthStatus.PENDING ||
                  _microsoftAuthService.AuthStatus == EAuthStatus.NONE))
            {
                await StopListeningAsync(cancellationToken);
                break;
            }

            // Checks the device code and updates the next check time.
            await _microsoftAuthService.CheckDeviceCodeAsync(deviceCode, cancellationToken);
            await Task.Delay(interval, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopListeningAsync(CancellationToken cancellationToken = default)
    {
        _isListening = false;
        return Task.CompletedTask;
    }
}
