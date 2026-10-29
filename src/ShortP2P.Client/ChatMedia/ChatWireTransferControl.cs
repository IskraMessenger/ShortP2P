namespace ShortP2P.Client.ChatMedia;

public sealed record ChatWireTransferControl(
    string Command,
    string TransferId,
    string TransferToken,
    string Host,
    int Port,
    long ExpiresUtcTicks,
    string ErrorCode) : ChatWireMessage;
