namespace ShortP2P.MessengerServer.Domain;

/// <summary>
/// Encrypted bot → client message held until the client receives it and submits a delivery receipt.
/// Ciphertext is opaque to the server.
/// </summary>
public sealed class BotToClientMessage
{
    public required string MessageId { get; init; }

    public required string BotNetworkId { get; init; }

    public required string ClientNetworkId { get; init; }

    /// <summary>Creation time UTC.</summary>
    public required DateTime CreatedUtc { get; init; }

    /// <summary>Opaque ciphertext, base64.</summary>
    public required string EncryptedMessageBase64 { get; init; }

    /// <summary>Optional end-to-end correlation id. Relayed unchanged.</summary>
    public string? CorrelationId { get; init; }
}
