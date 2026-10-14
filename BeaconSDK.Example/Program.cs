using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK.Models.Config;

namespace Tavstal.BeaconSDK.Example;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args);
        hostBuilder.SetupLogger();
        var config = new BeaconConfiguration
        {
            Auth = new AuthConfiguration
            {
                MicrosoftClientId = string.Empty, // Can be empty because this example does not use microsoft authentication
            }
        };
        hostBuilder = hostBuilder.ConfigureServices(services =>
        {
            services.AddBeaconServices(config);
            services.AddHostedService<ExampleWorker>();
        });
        var host = hostBuilder.Build();
        await host.RunAsync();
    }
}
