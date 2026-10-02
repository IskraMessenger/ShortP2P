using System.Text.Json;
using System.Text.Json.Serialization;
using ShortP2P.Discovery;

namespace ShortP2P.Client.ChatMedia;

/// <summary>Лимиты вложений в чат. JSON-файл по умолчанию рядом с приложением: <c>chat-media.json</c>.</summary>
public sealed class ChatMediaOptions
{
    /// <summary>Прежний лимит изображения в ультраэкономии (суперэкономия).</summary>
    public const int SuperEconomyMaxImageBytes = 100 * 1024;

    /// <summary>Прежний лимит документа в ультраэкономии (суперэкономия).</summary>
    public const int SuperEconomyMaxDocumentBytes = 200 * 1024;

    /// <summary>Прежний лимит видео в ультраэкономии: раньше совпадал с лимитом документа.</summary>
    public const int SuperEconomyMaxVideoBytes = SuperEconomyMaxDocumentBytes;

    /// <summary>Прежний лимит голоса в ультраэкономии: раньше совпадал с лимитом документа.</summary>
    public const int SuperEconomyMaxVoiceBytes = SuperEconomyMaxDocumentBytes;

    /// <summary>Прежняя максимальная длительность голосовой записи в ультраэкономии: 2 минуты.</summary>
    public const int SuperEconomyMaxVoiceSeconds = 2 * 60;

    /// <summary>Максимальная длительность голосовой записи вне суперэкономии: 10 минут.</summary>
    public const int MaxVoiceSeconds = 10 * 60;

    /// <summary>Изображение вне суперэкономии: 10 МБ.</summary>
    public const int DefaultMaxImageBytes = 10 * 1024 * 1024;

    /// <summary>Документ вне суперэкономии: 20 МБ.</summary>
    public const int DefaultMaxDocumentBytes = 20 * 1024 * 1024;

    /// <summary>Видео вне суперэкономии: 30 МБ.</summary>
    public const int DefaultMaxVideoBytes = 30 * 1024 * 1024;

    /// <summary>Голос вне суперэкономии: 1 МБ = 1 * 1024 * 1024.</summary>
    public const int DefaultMaxVoiceBytes = 1 * 1024 * 1024;

    private const int MinConfigurableImageBytes = 4 * 1024;
    private const int MaxConfigurableImageBytes = DefaultMaxImageBytes;
    private const int MinConfigurableDocumentBytes = 16 * 1024;
    private const int MaxConfigurableDocumentBytes = DefaultMaxDocumentBytes;
    private const int MessengerBinarySlackBytes = 256 * 1024;
    private const int BytesPerMegabyte = 1024 * 1024;

    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>Максимальный размер изображения вне суперэкономии: <see cref="DefaultMaxImageBytes"/>.</summary>
    public int MaxImageBytes { get; set; } = DefaultMaxImageBytes;

    /// <summary>Разрешённые MIME-типы изображений.</summary>
    public List<string> AllowedImageMimeTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/gif"
    ];

    /// <summary>Максимальный размер документа вне суперэкономии: <see cref="DefaultMaxDocumentBytes"/>.</summary>
    public int MaxDocumentBytes { get; set; } = DefaultMaxDocumentBytes;

    /// <summary>Максимальный размер видео вне суперэкономии: <see cref="DefaultMaxVideoBytes"/>.</summary>
    public int MaxVideoBytes { get; set; } = DefaultMaxVideoBytes;

    /// <summary>Максимальный размер голосового сообщения вне суперэкономии: <see cref="DefaultMaxVoiceBytes"/>.</summary>
    public int MaxVoiceBytes { get; set; } = DefaultMaxVoiceBytes;

    /// <summary>Верхняя граница размера расшифрованного бинарного кадра чата (крупнейшее вложение + заголовок wire).</summary>
    public int MaxMessengerBinaryBytes =>
        Math.Max(MaxVideoBytes, Math.Max(MaxDocumentBytes, MaxImageBytes)) + MessengerBinarySlackBytes;

    /// <summary>Разрешённые MIME для вложений-документов.</summary>
    public List<string> AllowedDocumentMimeTypes { get; set; } =
    [
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-powerpoint",
        "application/vnd.oasis.opendocument.text",
        "application/vnd.oasis.opendocument.spreadsheet",
        "application/vnd.oasis.opendocument.presentation",
        "application/vnd.oasis.opendocument.graphics",
        "application/rtf",
        "application/pdf",
        "video/mp4",
        "video/x-msvideo",
        "video/quicktime",
        "video/x-ms-wmv",
        "video/ogg",
        "video/webm",
        "audio/ogg"
    ];

    /// <summary>
    /// Суперэкономия — <see cref="TrafficQualityMode.UltraEconomy"/> (ультраэкономия).
    /// Нормальный режим и экономия используют расширенные лимиты.
    /// </summary>
    public static bool IsSuperEconomy(TrafficQualityMode mode) =>
        mode == TrafficQualityMode.UltraEconomy;

    public int GetMaxImageBytes(TrafficQualityMode mode) =>
        IsSuperEconomy(mode) ? SuperEconomyMaxImageBytes : MaxImageBytes;

    public int GetMaxDocumentBytes(TrafficQualityMode mode) =>
        IsSuperEconomy(mode) ? SuperEconomyMaxDocumentBytes : MaxDocumentBytes;

    public int GetMaxVideoBytes(TrafficQualityMode mode) =>
        IsSuperEconomy(mode) ? SuperEconomyMaxVideoBytes : MaxVideoBytes;

    public int GetMaxVoiceBytes(TrafficQualityMode mode) =>
        IsSuperEconomy(mode) ? SuperEconomyMaxVoiceBytes : MaxVoiceBytes;

    public int GetMaxVoiceSeconds(TrafficQualityMode mode) =>
        IsSuperEconomy(mode) ? SuperEconomyMaxVoiceSeconds : MaxVoiceSeconds;

    public static string FormatByteLimit(int bytes)
    {
        if (bytes >= BytesPerMegabyte && bytes % BytesPerMegabyte == 0)
            return $"{bytes / BytesPerMegabyte} МБ";
        return $"{(bytes + 1023) / 1024} КБ";
    }

    public static ChatMediaOptions LoadOrDefault(string? jsonPath)
    {
        var o = new ChatMediaOptions();
        if (string.IsNullOrWhiteSpace(jsonPath) || !File.Exists(jsonPath))
            return o;

        try
        {
            var json = File.ReadAllText(jsonPath);
            var dto = JsonSerializer.Deserialize<ChatMediaFileDto>(json, JsonRead);
            if (dto == null)
                return o;
            if (dto.MaxImageBytes is >= MinConfigurableImageBytes and <= MaxConfigurableImageBytes)
                o.MaxImageBytes = dto.MaxImageBytes.Value;
            if (dto.MaxDocumentBytes is >= MinConfigurableDocumentBytes and <= MaxConfigurableDocumentBytes)
                o.MaxDocumentBytes = dto.MaxDocumentBytes.Value;
            if (dto.AllowedImageMimeTypes is { Count: > 0 } list)
                o.AllowedImageMimeTypes = list
                    .Select(s => s.Trim().ToLowerInvariant())
                    .Where(s => s.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            if (dto.AllowedDocumentMimeTypes is { Count: > 0 } docList)
                o.AllowedDocumentMimeTypes = docList
                    .Select(s => s.Trim().ToLowerInvariant())
                    .Where(s => s.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
        }
        catch
        {
            // ignore bad config
        }

        if (!o.AllowedDocumentMimeTypes.Any(a => string.Equals(a, "audio/ogg", StringComparison.OrdinalIgnoreCase)))
            o.AllowedDocumentMimeTypes.Add("audio/ogg");

        return o;
    }

    public void ValidateMime(string mimeType)
    {
        var m = mimeType.Trim().ToLowerInvariant();
        if (!AllowedImageMimeTypes.Any(a => string.Equals(a, m, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Unsupported image type: {mimeType}", nameof(mimeType));
    }

    public void ValidateSize(int byteLength, TrafficQualityMode mode)
    {
        if (byteLength <= 0)
            throw new ArgumentException("Image is empty.", nameof(byteLength));
        var limit = GetMaxImageBytes(mode);
        if (byteLength > limit)
            throw new ArgumentException($"Image exceeds limit ({limit} bytes).", nameof(byteLength));
    }

    public void ValidateDocumentMime(string mimeType)
    {
        var m = mimeType.Trim().ToLowerInvariant();
        if (!AllowedDocumentMimeTypes.Any(a => string.Equals(a, m, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Unsupported document type: {mimeType}", nameof(mimeType));
    }

    public void ValidateDocumentSize(int byteLength, TrafficQualityMode mode)
    {
        if (byteLength <= 0)
            throw new ArgumentException("Document is empty.", nameof(byteLength));
        var limit = GetMaxDocumentBytes(mode);
        if (byteLength > limit)
            throw new ArgumentException($"Document exceeds limit ({limit} bytes).", nameof(byteLength));
    }

    public void ValidateVideoSize(int byteLength, TrafficQualityMode mode)
    {
        if (byteLength <= 0)
            throw new ArgumentException("Video is empty.", nameof(byteLength));
        var limit = GetMaxVideoBytes(mode);
        if (byteLength > limit)
            throw new ArgumentException($"Video exceeds limit ({limit} bytes).", nameof(byteLength));
    }

    public void ValidateVoiceSize(int byteLength, TrafficQualityMode mode)
    {
        if (byteLength <= 0)
            throw new ArgumentException("Voice is empty.", nameof(byteLength));
        var limit = GetMaxVoiceBytes(mode);
        if (byteLength > limit)
            throw new ArgumentException($"Voice exceeds limit ({limit} bytes).", nameof(byteLength));
    }

    /// <summary>Видео и голос — свои лимиты; остальные файлы — лимит документа.</summary>
    public void ValidateFileSize(string mimeType, int byteLength, TrafficQualityMode mode)
    {
        var mime = mimeType.Trim();
        if (mime.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            ValidateVideoSize(byteLength, mode);
        else if (mime.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            ValidateVoiceSize(byteLength, mode);
        else
            ValidateDocumentSize(byteLength, mode);
    }

    private sealed class ChatMediaFileDto
    {
        [JsonPropertyName("maxImageBytes")] public int? MaxImageBytes { get; set; }

        [JsonPropertyName("maxDocumentBytes")] public int? MaxDocumentBytes { get; set; }

        [JsonPropertyName("allowedImageMimeTypes")]
        public List<string>? AllowedImageMimeTypes { get; set; }

        [JsonPropertyName("allowedDocumentMimeTypes")]
        public List<string>? AllowedDocumentMimeTypes { get; set; }
    }
}
