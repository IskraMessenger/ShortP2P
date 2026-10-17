namespace ShortP2P.MessengerServer.Contracts;

/// <summary>Limits for store-and-forward encrypted chat / bot-relay messages.</summary>
public static class MessageLimits
{
    /// <summary>Max decoded ciphertext size for peer and bot-relay messages (not attachment blobs): 256 KiB.</summary>
    public const int MaxCiphertextBytes = 262_144;
}
