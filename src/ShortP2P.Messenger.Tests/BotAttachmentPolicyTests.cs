using ShortP2P.Client.ChatMedia;
using ShortP2P.Discovery;
using ShortP2P.MessengerServer.Contracts;

namespace ShortP2P.Messenger.Tests;

public class BotAttachmentPolicyTests
{
    [Theory]
    [InlineData("application/vnd.ms-powerpoint")]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    [InlineData("application/vnd.oasis.opendocument.presentation")]
    [InlineData("application/rtf")]
    [InlineData("video/x-msvideo")]
    [InlineData("video/quicktime")]
    [InlineData("video/webm")]
    [InlineData("image/webp")]
    [InlineData("audio/mpeg")]
    public void Send_AllowsAddedTypes(string mime)
    {
        BotAttachmentPolicy.ValidateSend(mime, 32, TrafficQualityMode.Normal, new ChatMediaOptions());
    }

    [Fact]
    public void NormalAndEconomy_UseBotCeilings()
    {
        var options = new ChatMediaOptions();
        Assert.Equal(BotAttachmentPolicy.MaxDocumentBytes,
            BotAttachmentPolicy.MaxSendBytes(BotAttachmentKind.Document, TrafficQualityMode.Normal, options));
        Assert.Equal(BotAttachmentPolicy.MaxDocumentBytes,
            BotAttachmentPolicy.MaxSendBytes(BotAttachmentKind.Document, TrafficQualityMode.Economy, options));
        Assert.Equal(BotAttachmentPolicy.MaxVideoBytes,
            BotAttachmentPolicy.MaxSendBytes(BotAttachmentKind.Video, TrafficQualityMode.Economy, options));
    }

    [Fact]
    public void UltraEconomy_ShrinksBelowCategoryCeiling()
    {
        var options = new ChatMediaOptions();
        Assert.Equal(ChatMediaOptions.SuperEconomyMaxImageBytes,
            BotAttachmentPolicy.MaxSendBytes(BotAttachmentKind.Image, TrafficQualityMode.UltraEconomy, options));
        Assert.Equal(ChatMediaOptions.SuperEconomyMaxDocumentBytes,
            BotAttachmentPolicy.MaxSendBytes(BotAttachmentKind.Video, TrafficQualityMode.UltraEconomy, options));

        Assert.Throws<ArgumentException>(() => BotAttachmentPolicy.ValidateSend(
            "image/png",
            ChatMediaOptions.SuperEconomyMaxImageBytes + 1,
            TrafficQualityMode.UltraEconomy,
            options));
    }

    [Fact]
    public void ChatMediaJsonCeiling_LowersDocumentSend()
    {
        var options = new ChatMediaOptions { MaxDocumentBytes = 4 * 1024 };
        Assert.Equal(4 * 1024,
            BotAttachmentPolicy.MaxSendBytes(BotAttachmentKind.Document, TrafficQualityMode.Normal, options));
    }

    [Fact]
    public async Task Split_RetriesOnlyTheFailedChunk()
    {
        var payload = new byte[BlobLimits.ChunkSizeBytes + 3];
        payload[0] = 1;
        payload[^1] = 9;
        var chunks = BotBlobChunker.Split(payload);
        Assert.Equal(2, chunks.Length);
        Assert.Equal(BlobLimits.ChunkSizeBytes, chunks[0].Length);
        Assert.Equal(3, chunks[1].Length);
        Assert.Equal(payload, BotBlobChunker.Join(chunks));

        var attempts = 0;
        await BotChunkRetry.RunAsync(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                    throw new InvalidOperationException("blip");
                return Task.CompletedTask;
            },
            delayForAttempt: _ => TimeSpan.Zero);

        Assert.Equal(3, attempts);
    }
}
