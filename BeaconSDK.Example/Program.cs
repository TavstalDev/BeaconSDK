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

        var config = new BeaconConfiguration
        {
            Auth = new AuthConfiguration
            {
                MicrosoftClientId = string.Empty, // Can be empty because this example does not use microsoft authentication
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
            services.AddHostedService<ExampleVanillaWorker>();
        });
        var host = hostBuilder.Build();
        await host.RunAsync();
    }
}
