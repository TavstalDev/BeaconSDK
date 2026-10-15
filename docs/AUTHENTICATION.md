# Authentication

`IMicrosoftAuthService` performs the full Microsoft → Xbox Live → XSTS → Minecraft
token exchange and produces an `Account` with encrypted tokens. The
`IMicrosoftDeviceAuthService` and `IMicrosoftHttpAuthService` drive two different
sign-in UX options.

---

## Before you start

Register an application in the Microsoft Azure portal as a **public client** (the
SDK sends no client secret) and add the redirect URI

```
http://localhost:43319/microsoft/authcallback
```

as a *Mobile and desktop applications* redirect. That path is built from
`MicrosoftHttpAuthService.ListeningUrl` plus `/microsoft/authcallback`, so it must
match what the SDK sends as `redirect_uri`. Then pass the client ID via configuration:

```csharp
var config = new BeaconConfiguration
{
    Auth = new AuthConfiguration
    {
        MicrosoftClientId = "your-azure-client-id"
    }
};
services.AddBeaconServices(config);
```

There is **no built-in fallback client ID**. `AuthConfiguration.MicrosoftClientId`
is a required property of your configuration: if it is empty or `null`, every
sign-in path logs an error and moves the status to `EAuthStatus.FAILED` (and
`OpenAuthenticationUrl` just logs an error without opening a browser). Leave it
empty only when your launcher never uses Microsoft login, such as offline mode.

Subscribe to status changes to update your UI:

```csharp
microsoftAuth.OnAuthStatusChanged += status =>
{
    // EAuthStatus: NONE, PENDING, PROCESSING, SUCCESS, FAILED
    Console.WriteLine($"Auth status: {status}");
};
```

After a successful sign-in, `microsoftAuth.AuthStatus == EAuthStatus.SUCCESS`,
`microsoftAuth.Account` holds the account, and `microsoftAuth.MojangProfile` holds
the Mojang profile.

---

## Option 1: Browser redirect flow

This flow opens the user's browser and listens on a loopback URL
(`http://localhost:43319/`) for the OAuth2 callback.

```csharp
// Start the local listener first (runs until auth succeeds or is stopped)
var listenTask = httpAuth.StartListeningAsync(progress, ct);

// Open the authorization page
microsoftAuth.OpenAuthenticationUrl();

// Optionally await completion
await listenTask;
```

`IMicrosoftHttpAuthService` automatically stops its listener once the SDK raises
`EAuthStatus.SUCCESS`.

> The loopback listener requires permission to bind `http://localhost:43319/`. On
> Windows the URL prefix may need to be reserved for the user; on some platforms
> running under a restricted account can fail. If starting the listener fails it
> reports `EProgressKind.AUTH_LISTENER_FAILED` and logs the error.

---

## Option 2: Device code flow

This flow needs no browser on the host machine: it shows a code and a URL that the
user opens on any other device. It suits consoles, smart TVs and headless servers.

```csharp
var deviceCode = await microsoftAuth.CreateDeviceCodeAsync(ct);
if (deviceCode is null) return;

Console.WriteLine($"Go to {deviceCode.VerificationUri} and enter {deviceCode.UserCode}");

// Poll until the user finishes signing in (or cancel via the token)
await deviceAuth.StartListeningAsync(deviceCode.DeviceCode, TimeSpan.FromSeconds(5), ct);
```

`DeviceCodeResult` exposes `DeviceCode`, `UserCode`, `VerificationUri`,
`ExpiresIn` and `Interval`. Microsoft returns a recommended poll interval, so you
can pass `TimeSpan.FromSeconds(deviceCode.Interval)` instead of a fixed value.

```csharp
await deviceAuth.StartListeningAsync(
    deviceCode.DeviceCode, config.Auth.DeviceCodePollInterval, ct);
```

`IMicrosoftDeviceAuthService.StartListeningAsync` polls
`IMicrosoftAuthService.CheckDeviceCodeAsync` at the given interval and stops
automatically once the status leaves `PENDING`/`NONE`. Calling it while a poll loop
is already running is a no-op; use `StopListeningAsync` to cancel early.

---

## Token lifecycle

`IMicrosoftAuthService` exposes the individual exchange steps (normally called for
you) as well as refresh:

| Method | Purpose |
| --- | --- |
| `CreateDeviceCodeAsync` | Requests a device code |
| `CheckDeviceCodeAsync` | Polls one device-code attempt |
| `HandleHttpRequestAsync` | Handles the OAuth2 redirect callback |
| `XboxTokenCallAsync` | Microsoft token → Xbox Live token |
| `XboxXstsCallAsync` | Xbox Live token → XSTS token |
| `MinecraftAccessCallAsync` | XSTS token → Minecraft access token |
| `CheckMinecraftOwnershipAsync` | Verifies game ownership |
| `GetMinecraftProfileAsync` | Fetches the profile and creates the `Account` |
| `FetchMinecraftProfileAsync` | Fetches a profile for an existing token |
| `RefreshLoginAsync` | Refreshes an expired login from a refresh token |
| `Reset` | Clears account and profile state |
| `OpenAuthenticationUrl` | Opens the Microsoft auth URL in browser |

The client ID is not part of the service surface; set it through
`BeaconConfiguration.Auth.MicrosoftClientId` before calling
`AddBeaconServices`.

Refresh a saved login:

```csharp
var refreshToken = account.GetRefreshToken(encryptionService);
var ok = await microsoftAuth.RefreshLoginAsync(
    System.Text.Encoding.UTF8.GetString(refreshToken), ct);
```

---

## Accounts and token encryption

`Account` stores tokens encrypted at rest. Access them through the encryption
service:

```csharp
// Read (decrypts)
byte[] access = account.GetAccessToken(encryptionService);
byte[] refresh = account.GetRefreshToken(encryptionService);

// Write (encrypts)
account.SetAccessToken(plainAccessToken, encryptionService);
account.SetRefreshToken(plainRefreshToken, encryptionService);
```

The obsolete `EncryptedAccessToken` / `EncryptedRefreshToken` properties are exposed
only for serialization; use the `Get*`/`Set*` methods instead.

`CanExpire` is `false` for `EAccountType.OFFLINE`.

---

## Offline accounts

For offline mode, build a `ClientDetails` with `isOffline: true` and derive a stable
UUID from the username:

```csharp
var uuid = GameHelper.GetOfflinePlayerUUID(username);
var client = new ClientDetails(accessToken: null, username, uuid, isOffline: true);
```

---

## Skins and capes

`IMojangSkinService` operates on a Minecraft access token:

```csharp
await skins.ChangeSkinAsync(mcToken, "classic", "https://example.com/skin.png", ct);
await skins.UploadSkinAsync(mcToken, "slim", "/path/to/skin.png", ct);
await skins.ResetSkinAsync(mcToken, playerId, ct);
await skins.ShowCapeAsync(mcToken, capeId, ct);
await skins.HideCapeAsync(mcToken, ct);
```

Each method returns the updated `MojangProfile`, or `null` on failure.
