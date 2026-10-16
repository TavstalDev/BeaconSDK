using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;
using Tavstal.BeaconSDK.Services.Abstractions.Auth;
using Tavstal.BeaconSDK.Tests.Fixtures;

namespace Tavstal.BeaconSDK.Tests.Services;

[Collection("TempDir")]
public class BeaconBootstrapTests : IDisposable
{
    private readonly string _keysDir;

    public BeaconBootstrapTests()
    {
        _keysDir = Path.Combine(Path.GetTempPath(), $"beacon-keys-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_keysDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_keysDir))
                Directory.Delete(_keysDir, true);
        }
        catch
        {
            /* best effort */
        }
    }

    private ServiceProvider BuildProvider()
    {
        var config = ModelFactory.CreateAppConfiguration();
        config.DataProtection.KeysDirectory = _keysDir;
        return new ServiceCollection().AddBeaconServices(config).BuildServiceProvider();
    }

    [Fact(DisplayName = "AddBeaconServices registers all expected service types")]
    public void AddBeaconServices_RegistersAllExpectedServiceTypes()
    {
        using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<IBeaconLogger<IHttpService>>());
        Assert.NotNull(provider.GetRequiredService<IHttpService>());
        Assert.NotNull(provider.GetRequiredService<IEncryptionService>());
        Assert.NotNull(provider.GetRequiredService<IManifestService>());
        Assert.NotNull(provider.GetRequiredService<ILibraryDownloadService>());
        Assert.NotNull(provider.GetRequiredService<IInstanceInstallService>());
        Assert.NotNull(provider.GetRequiredService<IInstanceLaunchService>());
        Assert.NotNull(provider.GetRequiredService<IMojangSkinService>());
        Assert.NotNull(provider.GetRequiredService<IMicrosoftAuthService>());
        Assert.NotNull(provider.GetRequiredService<IMicrosoftDeviceAuthService>());
        Assert.NotNull(provider.GetRequiredService<IMicrosoftHttpAuthService>());
        Assert.NotNull(provider.GetRequiredService<IJavaService>());
    }

    [Fact(DisplayName = "Resolved encryption service uses persisted keys from the temp key ring")]
    public void ResolvedEncryptionService_UsesPersistedKeys_FromTempKeyRing()
    {
        using var provider = BuildProvider();
        var encryption = provider.GetRequiredService<IEncryptionService>();
        var plain = "persisted-secret"u8.ToArray();

        var cipher = encryption.Encrypt(plain);

        Assert.Equal(plain, encryption.Decrypt(cipher));
        Assert.NotEmpty(Directory.EnumerateFiles(_keysDir, "*.xml"));
    }

    [Fact(DisplayName = "Resolved services are singletons returning the same instance on repeated resolve")]
    public void ResolvedServices_AreSingletons_SameInstanceOnRepeatedResolve()
    {
        using var provider = BuildProvider();

        Assert.Same(provider.GetRequiredService<IHttpService>(),
            provider.GetRequiredService<IHttpService>());
        Assert.Same(provider.GetRequiredService<IEncryptionService>(),
            provider.GetRequiredService<IEncryptionService>());
        Assert.Same(provider.GetRequiredService<IMicrosoftAuthService>(),
            provider.GetRequiredService<IMicrosoftAuthService>());
    }

    [Fact(DisplayName = "SetupLogger second call is a no-op")]
    public void SetupLogger_SecondCall_IsNoOp()
    {
        var builder = new HostBuilder();
        using var cts = new CancellationTokenSource();

        builder.SetupLogger(cts.Token);
        var first = BeaconBootstrap.LoggerTask;

        builder.SetupLogger(cts.Token);

        Assert.Same(first, BeaconBootstrap.LoggerTask);
        cts.Cancel();
    }
}
