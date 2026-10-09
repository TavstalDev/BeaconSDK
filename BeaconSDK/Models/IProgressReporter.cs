namespace Tavstal.BeaconSDK.Models;

/// <summary>
///     Represents a progress reporter interface for tracking and displaying progress and status updates.
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    ///     Reports the overall progress of the current operation.
    /// </summary>
    /// <param name="progress">The progress percentage in the range from 0 to 100.</param>
    void Report(double progress);

    /// <summary>
    ///     Reports the progress of a single file transfer or read operation.
    /// </summary>
    /// <param name="progress">The progress percentage in the range from 0 to 100.</param>
    /// <param name="fileName">The name of the file being processed.</param>
    /// <param name="downloading">
    ///     <see langword="true" /> if the file is being downloaded; <see langword="false" /> if it is being read.
    /// </param>
    void Report(double progress, string fileName, bool downloading = true);

    /// <summary>
    ///     Reports the progress of a categorized operation step.
    /// </summary>
    /// <param name="kind">The category of the operation being reported.</param>
    /// <param name="progress">The progress percentage in the range from 0 to 100.</param>
    void Report(EProgressKind kind, double progress);

    /// <summary>
    ///     Reports the expected number of tasks for a multi-step operation.
    /// </summary>
    /// <param name="count">The expected number of tasks, or <see langword="null" /> if unknown.</param>
    void ReportExpectedTask(int? count);

    /// <summary>
    ///     Reports that a single task within a multi-step operation has been completed.
    /// </summary>
    void ReportTaskCompleted();
}
