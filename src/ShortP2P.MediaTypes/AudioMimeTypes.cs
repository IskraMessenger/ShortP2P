namespace ShortP2P.MediaTypes;

/// <summary>MIME-type catalog for audio attachments.</summary>
public static class AudioMimeTypes
{
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg",
        "audio/ogg",
        "audio/mp4",
        "audio/x-m4a"
    };
}
