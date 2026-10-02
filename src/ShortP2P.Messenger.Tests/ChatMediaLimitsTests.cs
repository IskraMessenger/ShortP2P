using ShortP2P.Client.ChatMedia;
using ShortP2P.Discovery;

namespace ShortP2P.Messenger.Tests;

public class ChatMediaLimitsTests
{
    [Theory]
    [InlineData(TrafficQualityMode.Normal)]
    [InlineData(TrafficQualityMode.Economy)]
    public void NonSuperEconomy_UsesExtendedAttachmentLimits(TrafficQualityMode mode)
    {
        var options = new ChatMediaOptions();

        Assert.False(ChatMediaOptions.IsSuperEconomy(mode));
        Assert.Equal(1 * 1024 * 1024, 1_048_576);
        Assert.Equal(10 * 1024 * 1024, options.GetMaxImageBytes(mode));
        Assert.Equal(20 * 1024 * 1024, options.GetMaxDocumentBytes(mode));
        Assert.Equal(30 * 1024 * 1024, options.GetMaxVideoBytes(mode));
        Assert.Equal(1_048_576, options.GetMaxVoiceBytes(mode));
        Assert.Equal(10 * 60, options.GetMaxVoiceSeconds(mode));
        Assert.Equal("1 МБ", ChatMediaOptions.FormatByteLimit(options.GetMaxVoiceBytes(mode)));
        Assert.Equal("10 МБ", ChatMediaOptions.FormatByteLimit(options.GetMaxImageBytes(mode)));
        Assert.Equal("20 МБ", ChatMediaOptions.FormatByteLimit(options.GetMaxDocumentBytes(mode)));
        Assert.Equal("30 МБ", ChatMediaOptions.FormatByteLimit(options.GetMaxVideoBytes(mode)));

        options.ValidateSize(10 * 1024 * 1024, mode);
        options.ValidateDocumentSize(20 * 1024 * 1024, mode);
        options.ValidateVideoSize(30 * 1024 * 1024, mode);
        options.ValidateVoiceSize(1_048_576, mode);
        options.ValidateFileSize("video/mp4", 30 * 1024 * 1024, mode);
        options.ValidateFileSize("application/pdf", 20 * 1024 * 1024, mode);
        options.ValidateFileSize("audio/ogg", 1_048_576, mode);

        Assert.Throws<ArgumentException>(() => options.ValidateSize(10 * 1024 * 1024 + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateDocumentSize(20 * 1024 * 1024 + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateVideoSize(30 * 1024 * 1024 + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateVoiceSize(1_048_576 + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateFileSize("video/ogg", 30 * 1024 * 1024 + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateFileSize("audio/ogg", 1_048_576 + 1, mode));
    }

    [Fact]
    public void SuperEconomy_KeepsPreviousAttachmentLimits()
    {
        var options = new ChatMediaOptions();
        var mode = TrafficQualityMode.UltraEconomy;

        Assert.True(ChatMediaOptions.IsSuperEconomy(mode));
        Assert.Equal(100 * 1024, options.GetMaxImageBytes(mode));
        Assert.Equal(200 * 1024, options.GetMaxDocumentBytes(mode));
        Assert.Equal(200 * 1024, options.GetMaxVideoBytes(mode));
        Assert.Equal(200 * 1024, options.GetMaxVoiceBytes(mode));
        Assert.Equal(120, options.GetMaxVoiceSeconds(mode));

        options.ValidateSize(100 * 1024, mode);
        options.ValidateDocumentSize(200 * 1024, mode);
        options.ValidateVideoSize(200 * 1024, mode);
        options.ValidateVoiceSize(200 * 1024, mode);
        options.ValidateFileSize("video/mp4", 200 * 1024, mode);
        options.ValidateFileSize("audio/ogg", 200 * 1024, mode);

        Assert.Throws<ArgumentException>(() => options.ValidateSize(100 * 1024 + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateDocumentSize(200 * 1024 + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateVideoSize(200 * 1024 + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateVoiceSize(200 * 1024 + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateFileSize("application/pdf", 200 * 1024 + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateFileSize("audio/ogg", 200 * 1024 + 1, mode));
    }

    [Fact]
    public void JsonOverride_ChangesNonSuperEconomyOnly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"chat-media-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """
                {
                  "maxImageBytes": 8192,
                  "maxDocumentBytes": 20971520
                }
                """);

            var options = ChatMediaOptions.LoadOrDefault(path);

            Assert.Equal(8192, options.GetMaxImageBytes(TrafficQualityMode.Normal));
            Assert.Equal(8192, options.GetMaxImageBytes(TrafficQualityMode.Economy));
            Assert.Equal(20 * 1024 * 1024, options.GetMaxDocumentBytes(TrafficQualityMode.Normal));
            Assert.Equal(100 * 1024, options.GetMaxImageBytes(TrafficQualityMode.UltraEconomy));
            Assert.Equal(200 * 1024, options.GetMaxDocumentBytes(TrafficQualityMode.UltraEconomy));
            Assert.Equal(200 * 1024, options.GetMaxVideoBytes(TrafficQualityMode.UltraEconomy));
            Assert.Equal(30 * 1024 * 1024, options.GetMaxVideoBytes(TrafficQualityMode.Normal));
            Assert.Equal(1_048_576, options.GetMaxVoiceBytes(TrafficQualityMode.Normal));
            Assert.Equal(200 * 1024, options.GetMaxVoiceBytes(TrafficQualityMode.UltraEconomy));
            Assert.Equal(10 * 60, options.GetMaxVoiceSeconds(TrafficQualityMode.Economy));
            Assert.Equal(120, options.GetMaxVoiceSeconds(TrafficQualityMode.UltraEconomy));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void JsonOverride_AboveProductCeiling_IsIgnored()
    {
        var path = Path.Combine(Path.GetTempPath(), $"chat-media-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """
                {
                  "maxImageBytes": 10485761,
                  "maxDocumentBytes": 20971521
                }
                """);

            var options = ChatMediaOptions.LoadOrDefault(path);

            Assert.Equal(10 * 1024 * 1024, options.GetMaxImageBytes(TrafficQualityMode.Normal));
            Assert.Equal(20 * 1024 * 1024, options.GetMaxDocumentBytes(TrafficQualityMode.Economy));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
