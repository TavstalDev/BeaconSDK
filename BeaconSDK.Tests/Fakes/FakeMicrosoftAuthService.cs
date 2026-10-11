using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Accounts;
using Tavstal.BeaconSDK.Models.Microsoft;
using Tavstal.BeaconSDK.Models.MojangApi.User;
using Tavstal.BeaconSDK.Services.Abstractions.Auth;
using System.Net;

namespace Tavstal.BeaconSDK.Tests.Fakes;

/// <summary>
///     A programmable <see cref="IMicrosoftAuthService" /> used to drive the device/auth loops
///     without any network access.
/// </summary>
public sealed class FakeMicrosoftAuthService : IMicrosoftAuthService
{
    public EAuthStatus AuthStatus { get; private set; } = EAuthStatus.PENDING;
    public MojangProfile? MojangProfile { get; set; }
    public Account? Account { get; set; }

    /// <summary>Number of <see cref="CheckDeviceCodeAsync" /> calls before the status changes.</summary>
    public int PollsBeforeStatusChange { get; set; } = 1;

    /// <summary>Status applied once <see cref="PollsBeforeStatusChange" /> polls have happened.</summary>
    public EAuthStatus StatusAfterPolls { get; set; } = EAuthStatus.SUCCESS;

    public int CheckDeviceCodeCallCount { get; private set; }

    public event IMicrosoftAuthService.AuthStatusChangedHandler? OnAuthStatusChanged;

    public bool HasSubscribers => OnAuthStatusChanged != null;

    public void RaiseStatus(EAuthStatus status)
    {
        AuthStatus = status;
        OnAuthStatusChanged?.Invoke(status);
    }

    public void SetClientId(string clientId)
    {
    }

    public void Reset()
    {
        AuthStatus = EAuthStatus.NONE;
        Account = null;
        MojangProfile = null;
        OnAuthStatusChanged?.Invoke(AuthStatus);
    }

    public void OpenAuthenticationUrl()
    {
    }

    public Task HandleHttpRequestAsync(HttpListenerRequest request, IProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<DeviceCodeResult?> CreateDeviceCodeAsync(IProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<DeviceCodeResult?>(new DeviceCodeResult("device", "user", "uri", 900, 5));
    }

    public Task CheckDeviceCodeAsync(string deviceCode, CancellationToken cancellationToken = default)
    {
        CheckDeviceCodeCallCount++;
        if (CheckDeviceCodeCallCount >= PollsBeforeStatusChange)
            AuthStatus = StatusAfterPolls;
        return Task.CompletedTask;
    }

    public Task XboxTokenCallAsync(string token, string refreshToken, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task XboxXstsCallAsync(string token, string refreshToken, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task MinecraftAccessCallAsync(string token, string refreshToken, string userHash,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task CheckMinecraftOwnershipAsync(string mcToken, string refreshToken, int expireSeconds,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task GetMinecraftProfileAsync(string mcToken, string refreshToken, int expireSecs,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<MojangProfile?> FetchMinecraftProfileAsync(string mcToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MojangProfile);
    }

    public Task<bool> RefreshLoginAsync(string token, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
