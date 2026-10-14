using System.Diagnostics;

namespace Tavstal.BeaconSDK.Helpers.Platform;

/// <summary>
///     Provides helper methods for starting external processes with optional standard output and error streaming.
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    ///     Starts a process using the provided <see cref="ProcessStartInfo" /> and optionally subscribes to its
    ///     standard output and error streams. The process is disposed automatically when it exits.
    /// </summary>
    /// <param name="startInfo">The <see cref="ProcessStartInfo" /> describing the process to start.</param>
    /// <param name="onDataReceived">
    ///     Optional handler invoked for each line written by the process to standard output. When supplied, standard
    ///     output is read asynchronously.
    /// </param>
    /// <param name="errorDataReceived">
    ///     Optional handler invoked for each line written by the process to standard error. When supplied, standard
    ///     error is read asynchronously.
    /// </param>
    /// <returns>The started <see cref="Process" /> instance.</returns>
    public static Process Start(ProcessStartInfo startInfo,  DataReceivedEventHandler?  onDataReceived = null,  DataReceivedEventHandler? errorDataReceived = null)
    {
        Process process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;
        process.Exited += (sender, _) =>
        {
            if (sender is Process p)
                p.Dispose();
        };
        if (onDataReceived != null)
        {
            process.OutputDataReceived += onDataReceived;
            process.BeginOutputReadLine();
        }

        if (errorDataReceived != null)
        {
            process.ErrorDataReceived += errorDataReceived;
            process.BeginErrorReadLine();
        }

        process.Start();
        return process;
    }
}
