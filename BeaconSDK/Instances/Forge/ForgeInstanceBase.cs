using System.Diagnostics;
using System.Text.Json;
using Tavstal.BeaconSDK.Helpers.Domain;
using Tavstal.BeaconSDK.Helpers.IO;
using Tavstal.BeaconSDK.Models;
using Tavstal.BeaconSDK.Models.Instance;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge;
using Tavstal.BeaconSDK.Models.ModLoaders.Forge.Modern;

namespace Tavstal.BeaconSDK.Instances.Forge;

/// <summary>
///     Represents the base class for a Forge instance, inheriting from MinecraftInstance.
/// </summary>
/// <param name="id">Unique instance identifier.</param>
/// <param name="configuration">The configuration describing the game, paths, launcher and client settings for the instance.</param>
/// <param name="logger">The logger instance for logging information and errors.</param>
/// <param name="progressReporter">Optional progress reporter for tracking installation or setup progress.</param>
public abstract class ForgeInstanceBase(
    string id,
    InstanceConfiguration configuration,
    IBeaconLogger logger,
    IProgressReporter? progressReporter = null)
    : MinecraftInstance(id, configuration, logger, progressReporter)
{
    // The following methods are originally from https://github.com/CmlLib
    // but have been adapted to fit the current code structure and conventions.

    /// <summary>
    ///     Maps processor data and starts the processors for a Forge installation.
    /// </summary>
    /// <param name="installProfile">The Forge version profile containing installation data.</param>
    /// <param name="installerDir">The directory where the installer is located.</param>
    protected async Task MapAndStartProcessors(ForgeVersionProfile installProfile, string installerDir)
    {
        // Maps the processor data for the client side.
        Dictionary<string, string?> mapData = [];
        if (installProfile.Data != null)
            mapData = MapProcessorData(installProfile.Data.Value, VersionData.VanillaJarPath, installerDir);
        // Starts the processors using the mapped data.
        await StartProcessors(installProfile.Processors, mapData);
    }

    /// <summary>
    ///     Maps processor data to a dictionary for use during installation.
    /// </summary>
    /// <param name="data">The JSON object containing processor data.</param>
    /// <param name="minecraftJar">The path to the Minecraft jar file.</param>
    /// <param name="installDir">The directory where the installation is taking place.</param>
    /// <returns>A dictionary containing the mapped processor data.</returns>
    protected Dictionary<string, string?> MapProcessorData(JsonElement data, string minecraftJar, string installDir)
    {
        var dataMapping = new Dictionary<string, string?>();
        // Iterates through the data and maps each item to its full path
        foreach (var item in data.EnumerateObject())
        {
            var key = item.Name;
            var value = item.Value.GetProperty("client").GetString();

            if (string.IsNullOrEmpty(value))
                continue;

            var fullPath = ForgeMapper.ToFullPath(value, Config.Path.LibrariesDir);
            dataMapping[key] = fullPath == value
                ? Path.Combine(installDir, value.Trim('/'))
                : fullPath;
        }

        // Adds additional required mappings.
        dataMapping.Add("SIDE", "client");
        dataMapping.Add("MINECRAFT_JAR", minecraftJar);

        var parentDir = Path.GetDirectoryName(installDir)!;
        dataMapping.Add("INSTALLER", Path.Combine(parentDir, "installer.jar"));

        return dataMapping;
    }

    /// <summary>
    ///     Starts the processors for the Forge installation.
    /// </summary>
    /// <param name="processors">The array of processors to execute.</param>
    /// <param name="mapData">The mapped processor data.</param>
    protected async Task StartProcessors(JsonElement processors, Dictionary<string, string?> mapData)
    {
        if (processors.ValueKind != JsonValueKind.Array)
            return;

        var count = processors.EnumerateArray().Count();
        double processed = 0;
        // Iterates through each processor and starts it if necessary.
        foreach (var processor in processors.EnumerateArray())
        {
            // Checks if the processor outputs are valid.
            if (processor.TryGetProperty("outputs", out var outputs) && CheckProcessorOutputs(outputs, mapData))
            {
                processed++;
                continue;
            }

            if (!processor.TryGetProperty("sides", out var sides) || CheckClientSides(sides))
                await StartProcessor(processor, mapData);

            // Updates the progress reporter with the current progress.
            var percent = processed / count * 100d;
            _progressReporter?.Report(EProgressKind.INSTANCE_BUILDING_FORGE, percent);
        }
    }

    /// <summary>
    ///     Checks if the client sides array contains any "server" entries.
    /// </summary>
    /// <param name="sides">The JSON element containing the client sides.</param>
    /// <returns>True if no "server" entries are found, otherwise false.</returns>
    private bool CheckClientSides(JsonElement sides)
    {
        if (sides.ValueKind == JsonValueKind.Array)
            foreach (var item in sides.EnumerateArray())
            {
                if (item.GetString() == "server")
                    return false;
                break;
            }

        return true;
    }

    /// <summary>
    ///     Checks if the processor outputs are valid by verifying file existence and SHA1 hashes.
    /// </summary>
    /// <param name="outputs">The JSON object containing the processor outputs.</param>
    /// <param name="mapData">The mapped processor data.</param>
    /// <returns>True if all outputs are valid; otherwise, false.</returns>
    private bool CheckProcessorOutputs(JsonElement outputs, Dictionary<string, string?> mapData)
    {
        if (outputs.ValueKind != JsonValueKind.Object)
            return true;

        foreach (var item in outputs.EnumerateObject())
        {
            // Interpolates the key and value using the mapped data.
            var key = ForgeMapper.Interpolation(item.Name, mapData, true);
            var value = ForgeMapper.Interpolation(item.Value.ToString(), mapData, true);

            // Verifies the file existence and SHA1 hash.
            if (!File.Exists(key) || !FileSystemHelper.CheckSHA1(key, value))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Starts a single processor by executing its associated jar file.
    /// </summary>
    /// <param name="processor">The JSON token representing the processor.</param>
    /// <param name="mapData">The mapped processor data.</param>
    private async Task StartProcessor(JsonElement processor, Dictionary<string, string?> mapData)
    {
        string? name = null;
        if (processor.TryGetProperty("jar", out var jarProp) && jarProp.ValueKind == JsonValueKind.String)
            name = jarProp.GetString();

        if (string.IsNullOrEmpty(name))
            return;

        var jarPath = Path.Combine(Config.Path.LibrariesDir, PackageName.Parse(name).GetPath());
        var jarManifest = new ProcessorJarFile(jarPath).GetManifest();

        if (jarManifest == null || !jarManifest.TryGetValue("Main-Class", out var mainClass) ||
            string.IsNullOrEmpty(mainClass))
            return;

        // Constructs the classpath for the processor.
        var classpath = new List<string>();
        if (processor.TryGetProperty("classpath", out var classpathProp) &&
            classpathProp.ValueKind == JsonValueKind.Array)
            foreach (var libName in classpathProp.EnumerateArray())
            {
                var libNameString = libName.GetString();
                if (string.IsNullOrEmpty(libNameString))
                    continue;

                var lib = Path.Combine(Config.Path.LibrariesDir, PackageName.Parse(libNameString).GetPath());
                classpath.Add(lib);
            }

        classpath.Add(jarPath);

        // Constructs the arguments for the processor.
        string[] args = [];
        if (processor.TryGetProperty("args", out var argsProp) &&
            argsProp.ValueKind == JsonValueKind.Array)
        {
            var arrStrs = argsProp.EnumerateArray().Select(x => x.ToString()).ToArray();
            args = ForgeMapper.Map(arrStrs, mapData, Config.Path.LibrariesDir);
        }


        await StartJava(classpath.ToArray(), mainClass, args);
    }

    /// <summary>
    ///     Starts a Java process with the specified classpath, main class, and arguments.
    /// </summary>
    /// <param name="classpath">The array of classpath entries.</param>
    /// <param name="mainClass">The main class to execute.</param>
    /// <param name="args">The arguments to pass to the Java process.</param>
    private async Task StartJava(string[] classpath, string mainClass, string[]? args)
    {
        var defaultJava = string.IsNullOrEmpty(Config.Game.JavaPath) ? "java" : Config.Game.JavaPath;
        if (string.IsNullOrEmpty(defaultJava))
            throw new InvalidOperationException("JavaPath was empty");

        _logger.LogDebug($"Forge processor java: {defaultJava}");

        // Constructs the combined classpath string.
        var combinedPath = string.Join(Path.PathSeparator.ToString(),
            classpath.Select(x =>
            {
                var path = Path.GetFullPath(x);
                if (path.Contains(' '))
                    return "\"" + path + "\"";
                return path;
            }));

        // Constructs the argument string for the Java process.
        _logger.LogDebug("Combined classpath: " + combinedPath);
        _logger.LogDebug("Main class: " + mainClass);
        var arg = $"-cp {combinedPath} {mainClass}";

        if (args is { Length: > 0 })
        {
            arg += " " + string.Join(" ", args);
            _logger.LogDebug("Arguments: " + arg.Replace(combinedPath, "[classpath_placeholder]"));
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = defaultJava, Arguments = arg, RedirectStandardError = true
            }
        };
        process.Start();
        await process.WaitForExitAsync();

#if DEBUG
        var o = await process.StandardError.ReadToEndAsync();
        if (!string.IsNullOrEmpty(o))
            _logger.LogError("Forge processor error: " + o);
#endif
    }
}
