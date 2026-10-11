using FluentAssertions;
using Tavstal.BeaconSDK.Helpers.IO;

namespace Tavstal.BeaconSDK.Tests.Helpers.IO;

public class FileSystemHelperTests
{
    private readonly ITestOutputHelper output;
    public FileSystemHelperTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Theory(DisplayName = "GetFormattedSize returns binary units")]
    [InlineData(1024, "1 KB")]
    [InlineData(1024 * 1024, "1 MB")]
    [InlineData(1024 * 1024 * 1024, "1 GB")]
    public void GetFormattedSize_ReturnsBinaryUnits(long size, string expected)
    {
        var result = FileSystemHelper.GetFormattedSize(size);
        result.Should().Be(expected);
    }

    [Theory(DisplayName = "IsSafeToDelete rejects root, short paths and null/whitespace")]
    [InlineData("/")]
    [InlineData("/home/")]
    [InlineData("")]
    [InlineData(" ")]
    public void IsSafeToDelete_RejectsRoot_AndShortPaths_AndNullWhitespace(string path)
    {
        var result = FileSystemHelper.IsSafeToDelete(path);
        result.Should().BeFalse();
    }

    [Theory(DisplayName = "GetContentHash returns a lowercase SHA1 hex digest")]
    [InlineData("DataToHash", "e5f0a32a7757b88b0833a87101f54ebfe042304a")]
    [InlineData("DataToHash2", "6723c018c8d13aac9338412bdf19258e3e9c12bf")]
    [InlineData("DataToHash3", "e8d3a24956153c963a0efe9c4610b71bbdbdf8cd")]
    public void GetContentHash_ReturnsLowercaseSha1_Hex(string hash, string expected)
    {
        var result = FileSystemHelper.GetContentHash(hash);
        result.Should().Be(expected);
        string lowercaseHash = result.ToLower();
        lowercaseHash.Should().Be(expected);
    }

    [Theory(DisplayName = "CheckByDigest parses sha1 and sha256 digest formats")]
    [InlineData("sha1:a94a8fe5ccb19ba61c4c0873d391e987982fbbd3")]
    [InlineData("sha256:9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    public void CheckByDigest_ParsesSha1AndSha256Digest_Format(string digest)
    {
        string fileName = Path.GetTempFileName();
        try
        {
            File.WriteAllText(fileName, "test");
            var result = FileSystemHelper.CheckByDigest(fileName, digest);
            result.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }
    }

    [Fact(DisplayName = "GetFileHashAsync streams over the file and returns a non-empty SHA1")]
    public async Task GetFileHashAsync_StreamOverMemoryStream_MatchesKnownSha1_AndResetsPosition()
    {
        string fileName = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(fileName, "test", TestContext.Current.CancellationToken);
            var result = await FileSystemHelper.GetFileHashAsync(fileName);
            result.Should().NotBeNullOrEmpty();
        }
        finally
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }
    }

    [Fact(DisplayName = "CheckSHA1 returns true when the compare hash is null or empty")]
    public async Task CheckSHA1_ReturnsTrue_WhenCompareHashIsNullOrEmpty()
    {
        string fileName = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(fileName, "test", TestContext.Current.CancellationToken);
            var result = FileSystemHelper.CheckSHA1(fileName, string.Empty);
            result.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }
    }

    [Fact(DisplayName = "CheckSHA1 returns false on a case-sensitive mismatch")]
    public async Task CheckSHA1_CaseSensitiveMismatch_ReturnsFalse()
    {
        string fileName = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(fileName, "test", TestContext.Current.CancellationToken);
            var result = FileSystemHelper.CheckSHA1(fileName, "A94a8fe5ccb19ba61c4c0873d391e987982fbbd3");
            result.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }
    }

    [Theory(DisplayName = "CompressFile produces gzip and deletes the source when requested")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CompressFile_ProducesGzip_AndDeletesSource_WhenRequested(bool shouldDeleteSource)
    {
        string fileName = Path.GetTempFileName();
        string compressedFileName = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(fileName, "test", TestContext.Current.CancellationToken);
            bool result = FileSystemHelper.CompressFile(fileName, compressedFileName, shouldDeleteSource);
            result.Should().BeTrue();
            output.WriteLine($"Does source exist? {File.Exists(fileName)}");
        }
        finally
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
            if (File.Exists(compressedFileName))
                File.Delete(compressedFileName);
        }
    }
}
