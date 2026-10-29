namespace ShortP2P.MediaTypes;

/// <summary>MIME-type catalog for video attachments.</summary>
public static class VideoMimeTypes
{
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/ogg",
        "video/x-msvideo",
        "video/quicktime",
        "video/webm"
    };
}
