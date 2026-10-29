namespace ShortP2P.Client.Services;

public sealed class ChatMessageAppendedEventArgs(int chatId, bool outgoing) : EventArgs
{
    public int ChatId { get; } = chatId;
    public bool Outgoing { get; } = outgoing;
}
