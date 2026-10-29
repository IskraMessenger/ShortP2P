namespace ShortP2P.Client.Services;

public sealed class ChatCreatedEventArgs(int chatId, bool remote) : EventArgs
{
    public int ChatId { get; } = chatId;

    /// <summary>True — чат появился извне (invite / сервер), не создан вручную пользователем.</summary>
    public bool Remote { get; } = remote;
}
