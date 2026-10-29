namespace ShortP2P.Client.Services;

public sealed class PeerPublicKeyChangedEventArgs : EventArgs
{
    public int ChatId { get; init; }
    public string PeerNickname { get; init; } = "";
    public string PeerNetworkIdShort { get; init; } = "";
    public string PreviousSafetyNumber { get; init; } = "";
    public string NewSafetyNumber { get; init; } = "";
}
