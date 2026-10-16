using Tavstal.BeaconSDK.Helpers.Serialization;
using Tavstal.BeaconSDK.Models.Accounts;

namespace Tavstal.BeaconSDK.Tests.Helpers.Serialization;

public class JsonHelperTests
{
    [Fact(DisplayName = "WriteJsonFile then ReadJsonFile round-trips using CoreJsonContext")]
    public async Task WriteJsonFile_Then_ReadJsonFile_RoundTrips_UsingCoreJsonContext()
    {
        string tempFile  = Path.GetTempFileName();
        AccountSkin skin = new AccountSkin
        {
            Id = Guid.NewGuid().ToString(),
            Model = "wide"
        };
        try
        {
            bool result = await JsonHelper.WriteJsonFileAsync(tempFile, skin, BeaconJsonContext.Default.AccountSkin, TestContext.Current.CancellationToken);
            Assert.True(result);
            var readSkin = await JsonHelper.ReadJsonFileAsync(tempFile, BeaconJsonContext.Default.AccountSkin, TestContext.Current.CancellationToken);
            Assert.NotNull(readSkin);
            Assert.Equal(skin.Id, readSkin.Id);
            Assert.Equal(skin.Model, readSkin.Model);
            Assert.Equal(skin.CapeId, readSkin.CapeId);
            Assert.Equal(skin.MojangId, readSkin.MojangId);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact(DisplayName = "ReadJsonFile returns null when the file is missing")]
    public async Task ReadJsonFile_ReturnsDefault_WhenFileMissing()
    {
        string tempFile = Path.GetTempFileName();
        var result = await JsonHelper.ReadJsonFileAsync(tempFile, BeaconJsonContext.Default.AccountSkin, TestContext.Current.CancellationToken);
        Assert.Null(result);
    }
}
