using ShortP2P.Discovery;

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

    private static readonly HashSet<string> Documents = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/pdf",
        "application/vnd.oasis.opendocument.text",
        "application/vnd.oasis.opendocument.spreadsheet",
        "application/vnd.oasis.opendocument.presentation",
        "application/rtf",
        "application/x-abiword"
    };

    private static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp"
    };

    private static readonly HashSet<string> Videos = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/ogg",
        "video/x-msvideo",
        "video/quicktime",
        "video/webm"
    };

    private static readonly HashSet<string> Audio = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg",
        "audio/ogg",
        "audio/mp4",
        "audio/x-m4a"
    };

    public static bool TryClassify(string? mimeType, out BotAttachmentKind kind)
    {
        var mime = mimeType?.Trim() ?? "";
        if (Images.Contains(mime))
        {
            kind = BotAttachmentKind.Image;
            return true;
        }

        if (Audio.Contains(mime))
        {
            kind = BotAttachmentKind.Audio;
            return true;
        }

        if (Videos.Contains(mime))
        {
            kind = BotAttachmentKind.Video;
            return true;
        }

        if (Documents.Contains(mime))
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
