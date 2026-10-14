using FluentAssertions;
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

        provider.GetRequiredService<IBeaconLogger<IHttpService>>().Should().NotBeNull();
        provider.GetRequiredService<IHttpService>().Should().NotBeNull();
        provider.GetRequiredService<IEncryptionService>().Should().NotBeNull();
        provider.GetRequiredService<IManifestService>().Should().NotBeNull();
        provider.GetRequiredService<ILibraryDownloadService>().Should().NotBeNull();
        provider.GetRequiredService<IInstanceInstallService>().Should().NotBeNull();
        provider.GetRequiredService<IInstanceLaunchService>().Should().NotBeNull();
        provider.GetRequiredService<IMojangSkinService>().Should().NotBeNull();
        provider.GetRequiredService<IMicrosoftAuthService>().Should().NotBeNull();
        provider.GetRequiredService<IMicrosoftDeviceAuthService>().Should().NotBeNull();
        provider.GetRequiredService<IMicrosoftHttpAuthService>().Should().NotBeNull();
        provider.GetRequiredService<IJavaService>().Should().NotBeNull();
    }

    [Fact(DisplayName = "Resolved encryption service uses persisted keys from the temp key ring")]
    public void ResolvedEncryptionService_UsesPersistedKeys_FromTempKeyRing()
    {
        using var provider = BuildProvider();
        var encryption = provider.GetRequiredService<IEncryptionService>();
        var plain = "persisted-secret"u8.ToArray();

        var cipher = encryption.Encrypt(plain);

        encryption.Decrypt(cipher).Should().Equal(plain);
        Directory.EnumerateFiles(_keysDir, "*.xml").Should().NotBeEmpty();
    }

    [Fact(DisplayName = "Resolved services are singletons returning the same instance on repeated resolve")]
    public void ResolvedServices_AreSingletons_SameInstanceOnRepeatedResolve()
    {
        using var provider = BuildProvider();

        provider.GetRequiredService<IHttpService>().Should().BeSameAs(provider.GetRequiredService<IHttpService>());
        provider.GetRequiredService<IEncryptionService>().Should()
            .BeSameAs(provider.GetRequiredService<IEncryptionService>());
        provider.GetRequiredService<IMicrosoftAuthService>().Should()
            .BeSameAs(provider.GetRequiredService<IMicrosoftAuthService>());
    }

    [Fact(DisplayName = "SetupLogger second call is a no-op")]
    public void SetupLogger_SecondCall_IsNoOp()
    {
        var builder = new HostBuilder();
        using var cts = new CancellationTokenSource();

        builder.SetupLogger(cts.Token);
        var first = BeaconBootstrap.LoggerTask;

        builder.SetupLogger(cts.Token);

        BeaconBootstrap.LoggerTask.Should().BeSameAs(first);
        cts.Cancel();
    }
}
