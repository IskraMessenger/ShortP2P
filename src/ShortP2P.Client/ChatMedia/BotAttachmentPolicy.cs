using ShortP2P.Discovery;
using ShortP2P.MediaTypes;

namespace ShortP2P.Client.ChatMedia;

/// <summary>
/// Send-side limits for files addressed to a bot. Incoming files are not checked against these ceilings.
/// </summary>
public static class BotAttachmentPolicy
{
    public const int MaxDocumentBytes = 10 * 1024 * 1024;
    public const int MaxImageBytes = 10 * 1024 * 1024;
    public const int MaxVideoBytes = 50 * 1024 * 1024;
    public const int MaxAudioBytes = 10 * 1024 * 1024;

    public static bool TryClassify(string? mimeType, out BotAttachmentKind kind)
    {
        var mime = mimeType?.Trim() ?? "";
        if (ImageMimeTypes.All.Contains(mime))
        {
            kind = BotAttachmentKind.Image;
            return true;
        }

        if (AudioMimeTypes.All.Contains(mime))
        {
            kind = BotAttachmentKind.Audio;
            return true;
        }

        if (VideoMimeTypes.All.Contains(mime))
        {
            kind = BotAttachmentKind.Video;
            return true;
        }

        if (DocumentMimeTypes.All.Contains(mime))
        {
            kind = BotAttachmentKind.Document;
            return true;
        }

        kind = default;
        return false;
    }

    public static int CategoryMaxBytes(BotAttachmentKind kind) => kind switch
    {
        BotAttachmentKind.Image => MaxImageBytes,
        BotAttachmentKind.Document => MaxDocumentBytes,
        BotAttachmentKind.Video => MaxVideoBytes,
        BotAttachmentKind.Audio => MaxAudioBytes,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    /// <summary>
    /// Minimum of the bot category ceiling and the active traffic-quality limit.
    /// Normal and economy share byte ceilings; ultra-economy uses the super-economy caps.
    /// </summary>
    public static int MaxSendBytes(BotAttachmentKind kind, TrafficQualityMode mode, ChatMediaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var category = CategoryMaxBytes(kind);
        if (ChatMediaOptions.IsSuperEconomy(mode))
        {
            var ultra = kind == BotAttachmentKind.Image
                ? ChatMediaOptions.SuperEconomyMaxImageBytes
                : ChatMediaOptions.SuperEconomyMaxDocumentBytes;
            return Math.Min(category, ultra);
        }

        return kind switch
        {
            BotAttachmentKind.Image => Math.Min(category, options.MaxImageBytes),
            BotAttachmentKind.Document => Math.Min(category, options.MaxDocumentBytes),
            _ => category
        };
    }

    public static void ValidateSend(string mimeType, int byteLength, TrafficQualityMode mode, ChatMediaOptions options)
    {
        if (byteLength <= 0)
            throw new ArgumentException("File is empty.", nameof(byteLength));
        if (!TryClassify(mimeType, out var kind))
            throw new ArgumentException($"Unsupported attachment type: {mimeType}", nameof(mimeType));

        var limit = MaxSendBytes(kind, mode, options);
        if (byteLength > limit)
            throw new ArgumentException($"Attachment exceeds limit ({limit} bytes).", nameof(byteLength));
    }
}
