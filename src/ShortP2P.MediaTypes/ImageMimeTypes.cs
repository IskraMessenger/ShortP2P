namespace ShortP2P.MediaTypes;

/// <summary>MIME-type catalog for image attachments.</summary>
public static class ImageMimeTypes
{
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp"
    };
}
