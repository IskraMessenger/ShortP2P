using ShortP2P.Client.ChatMedia;
using ShortP2P.Discovery;

namespace ShortP2P.Messenger.Tests;

public class ChatMediaLimitsTests
{
    [Fact]
    public void LimitConstants_MatchBinaryMegabyteProducts()
    {
        Assert.Equal(100 * 1024, ChatMediaOptions.SuperEconomyMaxImageBytes);
        Assert.Equal(200 * 1024, ChatMediaOptions.SuperEconomyMaxDocumentBytes);
        Assert.Equal(ChatMediaOptions.SuperEconomyMaxDocumentBytes, ChatMediaOptions.SuperEconomyMaxVideoBytes);
        Assert.Equal(ChatMediaOptions.SuperEconomyMaxDocumentBytes, ChatMediaOptions.SuperEconomyMaxVoiceBytes);
        Assert.Equal(2 * 60, ChatMediaOptions.SuperEconomyMaxVoiceSeconds);
        Assert.Equal(10 * 60, ChatMediaOptions.MaxVoiceSeconds);
        Assert.Equal(10 * 1024 * 1024, ChatMediaOptions.DefaultMaxImageBytes);
        Assert.Equal(20 * 1024 * 1024, ChatMediaOptions.DefaultMaxDocumentBytes);
        Assert.Equal(60 * 1024 * 1024, ChatMediaOptions.DefaultMaxVideoBytes);
        Assert.Equal(1 * 1024 * 1024, ChatMediaOptions.DefaultMaxVoiceBytes);
    }

    [Theory]
    [InlineData(TrafficQualityMode.Normal)]
    [InlineData(TrafficQualityMode.Economy)]
    public void NonSuperEconomy_UsesExtendedAttachmentLimits(TrafficQualityMode mode)
    {
        var options = new ChatMediaOptions();

        Assert.False(ChatMediaOptions.IsSuperEconomy(mode));
        Assert.Equal(ChatMediaOptions.DefaultMaxImageBytes, options.GetMaxImageBytes(mode));
        Assert.Equal(ChatMediaOptions.DefaultMaxDocumentBytes, options.GetMaxDocumentBytes(mode));
        Assert.Equal(ChatMediaOptions.DefaultMaxVideoBytes, options.GetMaxVideoBytes(mode));
        Assert.Equal(ChatMediaOptions.DefaultMaxVoiceBytes, options.GetMaxVoiceBytes(mode));
        Assert.Equal(ChatMediaOptions.MaxVoiceSeconds, options.GetMaxVoiceSeconds(mode));
        Assert.Equal("1 МБ", ChatMediaOptions.FormatByteLimit(options.GetMaxVoiceBytes(mode)));
        Assert.Equal("10 МБ", ChatMediaOptions.FormatByteLimit(options.GetMaxImageBytes(mode)));
        Assert.Equal("20 МБ", ChatMediaOptions.FormatByteLimit(options.GetMaxDocumentBytes(mode)));
        Assert.Equal("60 МБ", ChatMediaOptions.FormatByteLimit(options.GetMaxVideoBytes(mode)));

        options.ValidateSize(options.GetMaxImageBytes(mode), mode);
        options.ValidateDocumentSize(options.GetMaxDocumentBytes(mode), mode);
        options.ValidateVideoSize(options.GetMaxVideoBytes(mode), mode);
        options.ValidateVoiceSize(options.GetMaxVoiceBytes(mode), mode);
        options.ValidateFileSize("video/mp4", options.GetMaxVideoBytes(mode), mode);
        options.ValidateFileSize("application/pdf", options.GetMaxDocumentBytes(mode), mode);
        options.ValidateFileSize("audio/ogg", options.GetMaxVoiceBytes(mode), mode);

        Assert.Throws<ArgumentException>(() => options.ValidateSize(options.GetMaxImageBytes(mode) + 1, mode));
        Assert.Throws<ArgumentException>(() =>
            options.ValidateDocumentSize(options.GetMaxDocumentBytes(mode) + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateVideoSize(options.GetMaxVideoBytes(mode) + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateVoiceSize(options.GetMaxVoiceBytes(mode) + 1, mode));
        Assert.Throws<ArgumentException>(() =>
            options.ValidateFileSize("video/ogg", options.GetMaxVideoBytes(mode) + 1, mode));
        Assert.Throws<ArgumentException>(() =>
            options.ValidateFileSize("audio/ogg", options.GetMaxVoiceBytes(mode) + 1, mode));
    }

    [Fact]
    public void SuperEconomy_KeepsPreviousAttachmentLimits()
    {
        var options = new ChatMediaOptions();
        var mode = TrafficQualityMode.UltraEconomy;

        Assert.True(ChatMediaOptions.IsSuperEconomy(mode));
        Assert.Equal(ChatMediaOptions.SuperEconomyMaxImageBytes, options.GetMaxImageBytes(mode));
        Assert.Equal(ChatMediaOptions.SuperEconomyMaxDocumentBytes, options.GetMaxDocumentBytes(mode));
        Assert.Equal(ChatMediaOptions.SuperEconomyMaxVideoBytes, options.GetMaxVideoBytes(mode));
        Assert.Equal(ChatMediaOptions.SuperEconomyMaxVoiceBytes, options.GetMaxVoiceBytes(mode));
        Assert.Equal(ChatMediaOptions.SuperEconomyMaxVoiceSeconds, options.GetMaxVoiceSeconds(mode));

        options.ValidateSize(options.GetMaxImageBytes(mode), mode);
        options.ValidateDocumentSize(options.GetMaxDocumentBytes(mode), mode);
        options.ValidateVideoSize(options.GetMaxVideoBytes(mode), mode);
        options.ValidateVoiceSize(options.GetMaxVoiceBytes(mode), mode);
        options.ValidateFileSize("video/mp4", options.GetMaxVideoBytes(mode), mode);
        options.ValidateFileSize("audio/ogg", options.GetMaxVoiceBytes(mode), mode);

        Assert.Throws<ArgumentException>(() => options.ValidateSize(options.GetMaxImageBytes(mode) + 1, mode));
        Assert.Throws<ArgumentException>(() =>
            options.ValidateDocumentSize(options.GetMaxDocumentBytes(mode) + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateVideoSize(options.GetMaxVideoBytes(mode) + 1, mode));
        Assert.Throws<ArgumentException>(() => options.ValidateVoiceSize(options.GetMaxVoiceBytes(mode) + 1, mode));
        Assert.Throws<ArgumentException>(() =>
            options.ValidateFileSize("application/pdf", options.GetMaxDocumentBytes(mode) + 1, mode));
        Assert.Throws<ArgumentException>(() =>
            options.ValidateFileSize("audio/ogg", options.GetMaxVoiceBytes(mode) + 1, mode));
    }

    [Fact]
    public void JsonOverride_ChangesNonSuperEconomyOnly()
    {
        const int configuredImageBytes = 8 * 1024;
        var path = Path.Combine(Path.GetTempPath(), $"chat-media-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, $$"""
                {
                  "maxImageBytes": {{configuredImageBytes}},
                  "maxDocumentBytes": {{ChatMediaOptions.DefaultMaxDocumentBytes}}
                }
                """);

            var options = ChatMediaOptions.LoadOrDefault(path);

            Assert.Equal(configuredImageBytes, options.GetMaxImageBytes(TrafficQualityMode.Normal));
            Assert.Equal(configuredImageBytes, options.GetMaxImageBytes(TrafficQualityMode.Economy));
            Assert.Equal(ChatMediaOptions.DefaultMaxDocumentBytes, options.GetMaxDocumentBytes(TrafficQualityMode.Normal));
            Assert.Equal(ChatMediaOptions.SuperEconomyMaxImageBytes, options.GetMaxImageBytes(TrafficQualityMode.UltraEconomy));
            Assert.Equal(ChatMediaOptions.SuperEconomyMaxDocumentBytes,
                options.GetMaxDocumentBytes(TrafficQualityMode.UltraEconomy));
            Assert.Equal(ChatMediaOptions.SuperEconomyMaxVideoBytes, options.GetMaxVideoBytes(TrafficQualityMode.UltraEconomy));
            Assert.Equal(ChatMediaOptions.DefaultMaxVideoBytes, options.GetMaxVideoBytes(TrafficQualityMode.Normal));
            Assert.Equal(ChatMediaOptions.DefaultMaxVoiceBytes, options.GetMaxVoiceBytes(TrafficQualityMode.Normal));
            Assert.Equal(ChatMediaOptions.SuperEconomyMaxVoiceBytes, options.GetMaxVoiceBytes(TrafficQualityMode.UltraEconomy));
            Assert.Equal(ChatMediaOptions.MaxVoiceSeconds, options.GetMaxVoiceSeconds(TrafficQualityMode.Economy));
            Assert.Equal(ChatMediaOptions.SuperEconomyMaxVoiceSeconds,
                options.GetMaxVoiceSeconds(TrafficQualityMode.UltraEconomy));
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
            File.WriteAllText(path, $$"""
                {
                  "maxImageBytes": {{ChatMediaOptions.DefaultMaxImageBytes + 1}},
                  "maxDocumentBytes": {{ChatMediaOptions.DefaultMaxDocumentBytes + 1}}
                }
                """);

            var options = ChatMediaOptions.LoadOrDefault(path);

            Assert.Equal(ChatMediaOptions.DefaultMaxImageBytes, options.GetMaxImageBytes(TrafficQualityMode.Normal));
            Assert.Equal(ChatMediaOptions.DefaultMaxDocumentBytes, options.GetMaxDocumentBytes(TrafficQualityMode.Economy));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
