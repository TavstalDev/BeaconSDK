using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Models.Config;

namespace Tavstal.BeaconSDK.Example;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args);

        var appLogsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        Directory.CreateDirectory(appLogsDir);
        string latestLog = Path.Combine(appLogsDir, "latest.log");
        var fileInfo = new FileInfo(latestLog);
        if (fileInfo.Exists)
        {
            var newPath = Path.Combine(appLogsDir, $"{fileInfo.LastWriteTime:yyyy-MM-dd_HH-mm-ss}.log");
            var archivePath = newPath + ".gz";
            File.Move(latestLog, newPath, true);
            FileSystemHelper.CompressFile(newPath, archivePath);
        }

        int workerIndex;
        while (true)
        {
            Console.WriteLine("Please enter the number of the worker you would like to start:");
            Console.WriteLine("0. Vanilla");
            Console.WriteLine("1. Fabric");
            Console.WriteLine("2. Quilt");
            Console.WriteLine("3. Forge");
            Console.WriteLine("4. NeoForge");
            string? response = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(response) || !int.TryParse(response, out workerIndex) || workerIndex < 0 ||
                workerIndex > 4)
            {
                Console.Clear();
                continue;
            }
            break;
        }

        var config = new BeaconConfiguration
        {
            Auth = new AuthConfiguration
            {
                MicrosoftClientId = string.Empty, // Can be empty because this example does not use Microsoft authentication
            },
            DataProtection = new DataProtectionConfig
            {
                WindowsEntropy  = [.. "8AE1C425-241D-46A9-A8F8-6E8A68D2586C"u8],
                LinuxKey        = [.. "5aEcj3cLUm5qYeCpptEaQoFCUTgZyEZw"u8],
                MacKey          = [.. "MqcM2kVeroKpvh9z79t3KjtiTzCUKSRs"u8]
            },
            Logging = new LoggingConfig
            {
                DefaultLogFilePath = latestLog
            }
        };
        hostBuilder = hostBuilder.ConfigureServices(services =>
        {
            services.AddBeaconServices(config);
            switch (workerIndex)
            {
                case 0:
                {
                    services.AddHostedService<ExampleVanillaWorker>();
                    break;
                }
                case 1:
                {
                    services.AddHostedService<ExampleFabricWorker>();
                    break;
                }
                case 2:
                {
                    services.AddHostedService<ExampleQuiltWorker>();
                    break;
                }
                case 3:
                {
                    services.AddHostedService<ExampleForgeWorker>();
                    break;
                }
                case 4:
                {
                    services.AddHostedService<ExampleNeoForgeWorker>();
                    break;
                }
            }
        });
        var host = hostBuilder.Build();
        await host.RunAsync();
    }
}
