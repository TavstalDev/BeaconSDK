using System.Text;
using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Domain;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Config;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;
using Tavstal.BeaconSDK.Services.Abstractions.Auth;
using Tavstal.BeaconSDK.Services.Implementations.Auth;

namespace Tavstal.BeaconSDK.Example;

public class ExampleLoginWorker  : IHostedService, IProgressReporter
{
    private readonly IBeaconLogger<ExampleLoginWorker> _logger;
    private readonly IMicrosoftAuthService  _microsoftAuthService;
    private readonly IMicrosoftDeviceAuthService _microsoftDeviceAuthService;
    private readonly IMicrosoftHttpAuthService _microsoftHttpAuthService;
    private readonly IEncryptionService _encryptionService;
    private readonly BeaconConfiguration _configuration;

    public string Uuid { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string? Token { get; private set; }

    public ExampleLoginWorker(IBeaconLogger<ExampleLoginWorker> logger, IMicrosoftAuthService microsoftAuthService,  IMicrosoftDeviceAuthService microsoftDeviceAuthService,
        IMicrosoftHttpAuthService microsoftHttpAuthService, IEncryptionService encryptionService, BeaconConfiguration configuration)
    {
        _logger = logger;
        _microsoftAuthService = microsoftAuthService;
        _microsoftDeviceAuthService = microsoftDeviceAuthService;
        _microsoftHttpAuthService = microsoftHttpAuthService;
        _encryptionService = encryptionService;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        bool isMicrosoftLogin;
        while (true)
        {
            _logger.LogInformation("Do you want to login with microsoft account? y/n");
            var c = Console.ReadKey().KeyChar;
            if (c == 'y')
            {
                isMicrosoftLogin = true;
                break;
            }

            if (c == 'n')
            {
                isMicrosoftLogin = false;
                break;
            }
        }

        if (!isMicrosoftLogin)
        {
            _logger.LogInformation("Please provide your minecraft username:");
            string? minecraftUsername;
            while (true)
            {
                minecraftUsername = Console.ReadLine();
                if (string.IsNullOrEmpty(minecraftUsername))
                {
                    _logger.LogInformation("Please provide a valid minecraft username:");
                    continue;
                }

                if (minecraftUsername.Length < 3 || minecraftUsername.Length > 16)
                {
                    _logger.LogInformation("Please provide a username that is between 3 and 16 characters:");
                    continue;
                }

                break;
            }

            Uuid = GameHelper.GetOfflinePlayerUUID(minecraftUsername);
            Username = minecraftUsername;
            Token = null;
            return;
        }

        // Start listening for authentication
        var deviceCode = await _microsoftAuthService.CreateDeviceCodeAsync(cancellationToken);
        if (deviceCode == null)
        {
            _logger.LogError("Failed to create device code.");
            return;
        }

        _microsoftAuthService.OnAuthStatusChanged += OnAuthStatusChanged;
        _logger.LogInformation($"Login at https://microsoft.com/link with '{deviceCode.UserCode}' code.");
        _logger.LogInformation($"Or open the following link: {_configuration.Endpoints.Microsoft.MakeMicrosoftAuthUrl(_configuration.Auth.MicrosoftClientId!, MicrosoftHttpAuthService.ListeningUrl)}");
        await Task.WhenAny(
            _microsoftDeviceAuthService.StartListeningAsync(deviceCode.DeviceCode,
                _configuration.Auth.DeviceCodePollInterval, cancellationToken),
            _microsoftHttpAuthService.StartListeningAsync(this, cancellationToken)
        );
    }

    private void OnAuthStatusChanged(EAuthStatus status)
    {
        if (status != EAuthStatus.SUCCESS || _microsoftAuthService.Account == null)
            return;

        Uuid = _microsoftAuthService.Account.Uuid;
        Username = _microsoftAuthService.Account.DisplayName;
        Token = Encoding.UTF8.GetString(_microsoftAuthService.Account.GetAccessToken(_encryptionService));
        _microsoftAuthService.Reset();

    }

    public Task StopAsync(CancellationToken cts)
    {
        _logger.LogInformation($"{nameof(ExampleLoginWorker)} has stopped");
        return Task.CompletedTask;
    }

    #region IProgressReporter Implementation

    public void Report(double progress)
    {
        _logger.LogInformation($"Progress: {progress:0.00}%");
    }

    public void Report(double progress, string fileName, bool downloading = true)
    {
        _logger.LogInformation(downloading
            ? $"Downloading {fileName} {progress:0.00}%"
            : $"Reading {fileName} {progress:0.00}%");
    }

    public void Report(EProgressKind kind, double progress)
    {
        _logger.LogInformation($"{kind}: {progress:0.00}%");
    }

    public void ReportExpectedTask(int? count) { }

    public void ReportTaskCompleted() { }

    #endregion
}
