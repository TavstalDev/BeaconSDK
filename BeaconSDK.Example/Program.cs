using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tavstal.BeaconSDK.Example;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args);
        hostBuilder.SetupLogger();
        hostBuilder = hostBuilder.ConfigureServices(services =>
        {
            services.AddBeaconServices("keysDir", "exampleApp");
            services.AddHostedService<ExampleWorker>();
        });
        var host = hostBuilder.Build();
        await host.RunAsync();
    }
}
