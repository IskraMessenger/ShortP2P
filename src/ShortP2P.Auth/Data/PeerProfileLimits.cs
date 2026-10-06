namespace ShortP2P.Auth.Data;

/// <summary>Лимиты полей профиля абонента (Avatar / AboutMe).</summary>
public static class PeerProfileLimits
{
    public const int MaxAboutMeChars = 250;

    /// <summary>Максимум UTF-8 байт для AboutMe на wire (250 символов × до 4 байт).</summary>
    public const int MaxAboutMeUtf8Bytes = MaxAboutMeChars * 4;

    /// <summary>Максимум байт аватара на wire / при новой записи (ChatWireUserInfo 0x06, discovery 0x45).</summary>
    public const int MaxAvatarBytes = 12 * 1024;

    /// <summary>
    ///     Потолок для чтения/показа уже сохранённых blob (старый лимит 20 KB).
    ///     Больше <see cref="MaxAvatarBytes"/> — на Get/Upsert пытаемся сжать до нового лимита.
    /// </summary>
    public const int MaxAvatarDisplayBytes = 20 * 1024;
}
