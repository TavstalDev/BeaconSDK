using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Tavstal.BeaconSDK.Enums;
using Tavstal.BeaconSDK.Helpers.Platform;
using Tavstal.BeaconSDK.Models.Logging;
using Tavstal.BeaconSDK.Services.Abstractions;
using Tavstal.BeaconSDK.Services.Implementations;

// ReSharper disable UnusedMember.Global

namespace Tavstal.BeaconSDK.Helpers.IO;

/// <summary>
///     Provides helper methods for file system operations such as deleting, moving directories, and verifying file hashes.
/// </summary>
public static class FileSystemHelper
{
    private static readonly IBeaconLogger _logger = new BeaconLogger(nameof(FileSystemHelper), LogLevel.Error);

    /// <summary>
    ///     Path of the temporal directory that is used by the sdk.
    /// </summary>
    public static readonly string TempDir = Path.Combine(Path.GetTempPath(), "beaconSdk");

    /// <summary>
    ///     Opens a folder in the system's default file explorer.
    /// </summary>
    /// <param name="path">The path to the folder to open.</param>
    public static void OpenFolderInFileExplorer(string path)
    {
        // Ensure the path exists
        if (!Directory.Exists(path))
        {
            _logger.LogError($"Error: Folder not found at '{path}'");
            return;
        }

        ProcessStartInfo? startInfo = null;
        switch (OSHelper.GetOperatingSystem())
        {
            case EOperatingSystem.WINDOWS:
            {
                startInfo = new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = false };
                startInfo.ArgumentList.Add(path);
                break;
            }
            case EOperatingSystem.MACOS:
            {
                startInfo = new ProcessStartInfo { FileName = "open", UseShellExecute = false };
                startInfo.ArgumentList.Add(path);
                break;
            }
            case EOperatingSystem.LINUX:
            {
                startInfo = new ProcessStartInfo { FileName = "xdg-open", UseShellExecute = false };
                startInfo.ArgumentList.Add(path);
                break;
            }
            case EOperatingSystem.UNKNOWN:
            {
                _logger.LogError("Error: Unsupported operating system for opening folder in file explorer.");
                break;
            }
        }

        if (startInfo == null)
            return;

        ProcessHelper.Start(startInfo);
    }

    /// <summary>
    ///     Converts a byte count into a human-readable string using binary (1024) units.
    /// </summary>
    /// <param name="size">Size in bytes.</param>
    /// <returns>
    ///     A formatted string like "123 B", "456 KB", "789 MB" etc.
    /// </returns>
    public static string GetFormattedSize(long size)
    {
        if (size < 1024)
            return $"{size} B";

        string[] units = ["KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];
        var bytes = size / 1024;

        for (var i = 0; i < units.Length; i++)
        {
            if (i == units.Length - 1 || bytes < 1024)
                return $"{bytes} {units[i]}";

            bytes /= 1024;
        }

        return $"{bytes} RB";
    }

    #region Core Operations

    /// <summary>
    ///     Deletes the file at the provided path if it exists, is not locked, and passes safety checks.
    /// </summary>
    /// <param name="path">Full path to the file to delete.</param>
    public static bool DeleteFile(string path)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
            return false;

        if (!File.Exists(path))
            return true; // Act like the file was deleted

        if (!MakeFileWritable(path))
        {
            _logger.LogError($"Failed to clear read-only attribute from file '{path}' before deletion.");
            return false;
        }

        if (IsFileLocked(path))
        {
            _logger.LogError($"Refusing to delete file '{path}' because it is currently locked by another process.");
            return false;
        }

        if (!IsSafeToDelete(path))
        {
            _logger.LogError("Refusing to delete file '{path}' because it is not considered safe to delete.");
            return false;
        }

        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Unexpected error while trying to delete file '{path}':");
            return false;
        }
    }

    /// <summary>
    ///     Deletes a directory and all its contents after verifying the directory is safe to delete.
    /// </summary>
    /// <param name="path">The path of the directory to delete.</param>
    public static bool DeleteDirectory(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
                return false;

            if (!Directory.Exists(path))
                return true;

            if (!IsSafeToDelete(path))
            {
                _logger.LogError($"Refusing to delete directory '{path}' because it is not considered safe to delete.");
                return false;
            }

            var homeDir = OSHelper.GetHomeDirectory();
            if (string.Equals(path, homeDir, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Refusing to delete directory '{path}' because it is the user's home directory.");
                return false;
            }

            var programsDir = OSHelper.GetProgramsDirectory();
            if (string.Equals(path, programsDir, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    $"Refusing to delete directory '{path}' because it is the system's programs directory.");
                return false;
            }

            var desktopDir = OSHelper.GetDesktopDirectory();
            if (string.Equals(path, desktopDir, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Refusing to delete directory '{path}' because it is the user's desktop directory.");
                return false;
            }

            var dirInfo = new DirectoryInfo(path);
            // Delete files
            var files = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
                if (!DeleteFile(file.FullName))
                {
                    _logger.LogError(
                        $"Failed to delete file '{file.FullName}' while deleting directory '{path}'. Aborting directory deletion.");
                    return false;
                }

            // Delete sub dirs
            var dirs = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToList();
            foreach (var dir in dirs)
                try
                {
                    var subDir = new DirectoryInfo(dir);
                    var attrs = subDir.Attributes;
                    if (attrs.HasFlag(FileAttributes.ReadOnly))
                        subDir.Attributes = attrs & ~FileAttributes.ReadOnly;
                    Directory.Delete(dir);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected error while trying to delete subdirectory '{dir}':");
                    return false;
                }

            Directory.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Unexpected error while trying to delete directory '{path}':");
            return false;
        }
    }

    /// <summary>
    ///     Moves a directory and its contents to a new location.
    /// </summary>
    /// <param name="sourceDir">The source directory path.</param>
    /// <param name="destinationDir">The destination directory path.</param>
    /// <param name="recursive">Indicates whether to move subdirectories recursively.</param>
    /// <param name="deleteSource">Indicates whether to delete the source directory after moving. Default is true.</param>
    /// <param name="overwrite">Indicates whether to overwrite existing files in the destination. Default is true.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if the source directory does not exist.</exception>
    public static void MoveDirectory(string sourceDir, string destinationDir, bool recursive, bool deleteSource = true,
        bool overwrite = true)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        var dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            if (overwrite || (!overwrite && !File.Exists(targetFilePath)))
                file.CopyTo(targetFilePath, true);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
            foreach (var subDir in dirs)
            {
                var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                MoveDirectory(subDir.FullName, newDestinationDir, true, false);
            }

        if (deleteSource)
            DeleteDirectory(sourceDir);
    }

    /// <summary>
    ///     Compresses a file to the specified destination using GZip compression.
    ///     Optionally deletes the source file after successful compression.
    /// </summary>
    /// <param name="source">Full path to the source file to compress.</param>
    /// <param name="destination">Full path to the destination compressed file to create.</param>
    /// <param name="deleteSource">If true, the source file will be deleted after compression completes (default: true).</param>
    /// <returns>
    ///     <c>true</c> when compression succeeded; <c>false</c> if the source file is locked and compression was not
    ///     attempted.
    /// </returns>
    public static bool CompressFile(string source, string destination, bool deleteSource = true)
    {
        if (IsFileLocked(source))
            return false;

        bool failed = false;
        try
        {
            using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read);
            using var targetStream = File.Create(destination);
            using var compressionStream = new GZipStream(targetStream, CompressionLevel.Optimal);

            sourceStream.CopyTo(compressionStream);
            return true;
        }
        catch (Exception)
        {
            failed = true;
            throw;
        }
        finally
        {
            if (deleteSource && !failed)
                DeleteFile(source);
        }
    }

    #endregion

    #region File Permissions and Attributes

    /// <summary>
    ///     Makes a file executable by modifying its permissions using the `chmod` command.
    /// </summary>
    /// <param name="path">The path of the file to make executable.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains a boolean value:
    ///     true if the operation succeeded, false otherwise.
    /// </returns>
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    public static bool MakeExecutable(string path)
    {
        try
        {
            if (OSHelper.GetOperatingSystem() == EOperatingSystem.UNKNOWN)
                return true;

            var currentMode = File.GetUnixFileMode(path);
            var executeFlags = UnixFileMode.UserExecute
                               | UnixFileMode.GroupExecute
                               | UnixFileMode.OtherExecute;

            File.SetUnixFileMode(path, currentMode | executeFlags);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to make '{path}' executable:");
            return false;
        }
    }

    /// <summary>
    ///     Sets the file at <paramref name="pathToFile" /> to read-only by adding the <see cref="FileAttributes.ReadOnly" />
    ///     flag.
    ///     If the file does not exist or is already read-only the method returns immediately.
    /// </summary>
    /// <param name="pathToFile">Full path to the file to mark as read-only.</param>
    public static bool MakeFileReadonly(string pathToFile)
    {
        try
        {
            if (!File.Exists(pathToFile))
                return false;

            var attributes = File.GetAttributes(pathToFile);
            if (attributes.HasFlag(FileAttributes.ReadOnly))
                return true;

            attributes |= FileAttributes.ReadOnly;
            File.SetAttributes(pathToFile, attributes);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to following file readonly {pathToFile}:");
            return false;
        }
    }

    /// <summary>
    ///     Clears the read-only flag from the file at <paramref name="pathToFile" />, making it writable.
    ///     If the file does not exist or is not read-only the method returns immediately.
    /// </summary>
    /// <param name="pathToFile">Full path to the file to make writable.</param>
    public static bool MakeFileWritable(string pathToFile)
    {
        try
        {
            if (!File.Exists(pathToFile))
                return false;

            var attributes = File.GetAttributes(pathToFile);
            if (!attributes.HasFlag(FileAttributes.ReadOnly))
                return true;

            attributes &= ~FileAttributes.ReadOnly;
            File.SetAttributes(pathToFile, attributes);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Failed to following file writable {pathToFile}:");
            return false;
        }
    }

    /// <summary>
    ///     Tests whether the application can write to the given directory by creating a small test file.
    ///     Preserves the original behavior: creates the directory if needed, creates "test.txt", verifies its existence,
    ///     deletes it if created, and returns true on success; logs and returns false on any exception.
    /// </summary>
    /// <param name="targetDir">Directory to test write permissions for.</param>
    /// <returns>True if a test file could be created and detected; otherwise false.</returns>
    public static bool HasWritePermission(string targetDir)
    {
        try
        {
            Directory.CreateDirectory(targetDir);

            var testFile = Path.Combine(targetDir, "test.txt");
            DeleteFile(testFile);

            File.WriteAllText(testFile, "test");
            var success = File.Exists(testFile);
            if (success)
                DeleteFile(testFile);
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while testing write permissions at {targetDir}:");
            return false;
        }
    }

    /// <summary>
    ///     Asynchronously tests whether the application has write permission to the given directory by attempting
    ///     to create a small test file ("test.txt") inside it.
    /// </summary>
    /// <param name="targetDir">
    ///     The directory to test write permissions for. The directory will be created if it does not
    ///     exist.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result is <c>true</c> if the test file could be created
    ///     and detected; otherwise <c>false</c>. Any exception is logged and results in <c>false</c>.
    /// </returns>
    public static async Task<bool> HasWritePermissionAsync(string targetDir)
    {
        try
        {
            Directory.CreateDirectory(targetDir);

            var testFile = Path.Combine(targetDir, "test.txt");
            DeleteFile(testFile);

            await File.WriteAllTextAsync(testFile, "test");
            var success = File.Exists(testFile);
            if (success)
                File.Delete(testFile);
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while testing write permissions at {targetDir}:");
            return false;
        }
    }

    /// <summary>
    ///     Checks whether the drive containing <paramref name="targetDir" /> has at least <paramref name="bytes" />
    ///     bytes of available free space.
    /// </summary>
    /// <param name="targetDir">The directory to check the drive of. The method will determine the root drive from this path.</param>
    /// <param name="bytes">The minimum number of free bytes required.</param>
    public static bool HasEnoughFreeSpace(string targetDir, long bytes)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(targetDir) ?? targetDir);
            return driveInfo.AvailableFreeSpace >= bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while checking free space at {targetDir}:");
            return false;
        }
    }

    #endregion

    #region Hashing

    /// <summary>
    ///     Verifies the SHA1 hash of a file against a given hash value.
    /// </summary>
    /// <param name="path">The path of the file to check.</param>
    /// <param name="compareHash">The SHA1 hash to compare against. If null or empty, the method returns true.</param>
    /// <returns>True if the file's hash matches the given hash; otherwise, false.</returns>
    public static bool CheckSHA1(string path, string? compareHash)
    {
        if (string.IsNullOrEmpty(compareHash))
            return true;

        try
        {
            string fileHash;
            using (var file = File.OpenRead(path))
            using (var hasher = SHA1.Create())
            {
                var binaryHash = hasher.ComputeHash(file);
                fileHash = Convert.ToHexStringLower(binaryHash);
            }

            return string.Equals(fileHash, compareHash);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to check SHA1 hash:");
            _logger.LogError(ex.ToString());
            return false;
        }
    }

    /// <summary>
    ///     Verifies the SHA256 hash of a file against a given hash value.
    /// </summary>
    /// <param name="path">The path of the file to check.</param>
    /// <param name="compareHash">
    ///     The SHA256 hash to compare against. If null or empty, the method returns true.
    /// </param>
    /// <returns>
    ///     True if the file's hash matches the given hash; otherwise, false.
    /// </returns>
    public static bool CheckSHA256(string path, string? compareHash)
    {
        if (string.IsNullOrEmpty(compareHash))
            return true;

        try
        {
            string fileHash;
            using (var file = File.OpenRead(path))
            using (var hasher = SHA256.Create())
            {
                var binaryHash = hasher.ComputeHash(file);
                fileHash = Convert.ToHexStringLower(binaryHash);
            }

            return string.Equals(fileHash, compareHash);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to check SHA256 hash:");
            _logger.LogError(ex.ToString());
            return false;
        }
    }

    /// <summary>
    ///     Verifies a file's hash using a digest string in the format "type:hash".
    ///     Supported digest types are "sha1" and "sha256" (case-insensitive).
    /// </summary>
    /// <param name="path">Filesystem path to the file to verify.</param>
    /// <param name="digest">
    ///     The digest string in the form "type:hash", for example "sha1:abcdef..." or "sha256:0123...".
    ///     The method splits on the first ':' and treats the left part as the digest type and the right part as the hex hash.
    /// </param>
    /// <returns>
    ///     <c>true</c> when the digest format is valid and the file's computed hash matches the provided hash;
    ///     <c>false</c> when the digest format is invalid, the digest type is unsupported, or the hash comparison fails.
    /// </returns>
    public static bool CheckByDigest(string path, string digest)
    {
        var parts = digest.Split(':', 2);
        if (parts.Length < 2)
        {
            _logger.LogError($"Invalid digest format: '{digest}'. Expected format is 'type:hash'.");
            return false;
        }

        switch (parts[0].ToLower())
        {
            case "sha1":
                return CheckSHA1(path, parts[1]);
            case "sha256":
                return CheckSHA256(path, parts[1]);
            default:
                _logger.LogError($"Unsupported digest type '{parts[0]}' in digest string '{digest}'.");
                return false;
        }
    }

    /// <summary>
    ///     Asynchronously computes the SHA-1 hash of the file at the given path and returns it as a lowercase hex string.
    /// </summary>
    /// <param name="path">Path to the file to hash.</param>
    public static async Task<string?> GetFileHashAsync(string path)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await GetFileHashAsync(stream);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to compute file hash:");
            _logger.LogError(ex.ToString());
            return null;
        }
    }

    /// <summary>
    ///     Computes the SHA-1 hash of the provided stream and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="stream">
    ///     The input stream to hash. The method reads from the current stream position to the end.
    ///     The caller is responsible for the stream's lifetime (this method does not dispose the provided stream).
    /// </param>
    public static async Task<string?> GetFileHashAsync(Stream stream)
    {
        try
        {
            using var sha = SHA1.Create();
            var hashBytes = await sha.ComputeHashAsync(stream);
            var fileHash = Convert.ToHexStringLower(hashBytes);
            stream.Position = 0;
            return fileHash;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to compute file hash:");
            _logger.LogError(ex.ToString());
            return null;
        }
    }

    /// <summary>
    ///     Computes the SHA-1 hash of the provided string using UTF-8 encoding and returns it as a lowercase hexadecimal
    ///     string.
    /// </summary>
    /// <param name="content">The input string to hash (encoded as UTF-8).</param>
    public static string? GetContentHash(string content)
    {
        try
        {
            using var sha = SHA1.Create();
            var contentBytes = Encoding.UTF8.GetBytes(content);
            using var stream = new MemoryStream(contentBytes);
            var hashBytes = sha.ComputeHash(stream);
            var contentHash = Convert.ToHexStringLower(hashBytes);
            return contentHash;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to compute content hash:");
            _logger.LogError(ex.ToString());
            return null;
        }
    }

    #endregion

    #region Safety

    /// <summary>
    ///     Checks whether the specified file is locked by attempting to open it with exclusive access.
    /// </summary>
    /// <param name="filePath">Full path to the file to test.</param>
    /// <returns>
    ///     <c>true</c> if the file could not be opened due to an <see cref="IOException" /> (commonly indicates the file is
    ///     locked);
    ///     <c>false</c> if the file was successfully opened with exclusive access.
    /// </returns>
    public static bool IsFileLocked(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
        catch (IOException)
        {
            return true;
        }
    }

    /// <summary>
    ///     Determines whether the specified path is considered safe for deletion.
    /// </summary>
    /// <param name="path">The path to evaluate.</param>
    /// <returns>
    ///     <c>true</c> when the path appears valid and not obviously unsafe to delete (e.g. not null/empty,
    ///     not a drive root and of reasonable length); otherwise <c>false</c>.
    /// </returns>
    public static bool IsSafeToDelete(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full);
        if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
            return false;
        if (full.Length < 10)
            return false;
        return true;
    }

    #endregion
}
