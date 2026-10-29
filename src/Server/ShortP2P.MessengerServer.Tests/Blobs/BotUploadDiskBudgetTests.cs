using ShortP2P.MessengerServer.UseCases.Blobs;

namespace ShortP2P.MessengerServer.Tests.Blobs;

public class BotUploadDiskBudgetTests
{
    [Theory]
    [InlineData(1000, 0, 800, true)]
    [InlineData(1000, 0, 801, false)]
    [InlineData(1000, 300, 500, true)]
    [InlineData(1000, 300, 501, false)]
    [InlineData(1000, 800, 1, false)]
    [InlineData(1000, 200, 300, true)]
    [InlineData(0, 0, 1, false)]
    [InlineData(9, 0, 7, true)]
    [InlineData(9, 0, 8, false)]
    [InlineData(5, 0, 4, true)]
    [InlineData(5, 0, 5, false)]
    public void Fits_UsesEightyPercentMinusOtherUploads(
        long freeBytes,
        long otherUploadsBytes,
        long incomingBytes,
        bool fits)
    {
        Assert.Equal(fits, BotUploadDiskBudget.Fits(freeBytes, otherUploadsBytes, incomingBytes));
    }

    [Fact]
    public void Fits_RejectsEmptyIncoming()
    {
        Assert.False(BotUploadDiskBudget.Fits(10_000, 0, 0));
        Assert.False(BotUploadDiskBudget.Fits(10_000, 0, -1));
    }

    [Fact]
    public void Fits_TreatsNegativeFreeOrReservedAsZero()
    {
        Assert.False(BotUploadDiskBudget.Fits(-100, 0, 1));
        Assert.True(BotUploadDiskBudget.Fits(1000, -50, 800));
    }

    [Fact]
    public void Fits_RejectsWhenOtherUploadsAloneExhaustBudget()
    {
        // 0.8 * 1000 = 800; reserved 800 leaves nothing for a new file.
        Assert.False(BotUploadDiskBudget.Fits(1000, 800, 1));
        Assert.False(BotUploadDiskBudget.Fits(1000, 900, 1));
    }
}
