namespace ShortP2P.MessengerServer.Domain;

/// <summary>
/// Encrypted client → bot message held until the bot's long-poll takes it.
/// Ciphertext is opaque to the server.
/// </summary>
public sealed class ClientToBotMessage
{
    public required string MessageId { get; init; }

    public required string ClientNetworkId { get; init; }

    public required string BotNetworkId { get; init; }

    /// <summary>Client-supplied timestamp UTC.</summary>
    public required DateTime CreatedUtc { get; init; }

    /// <summary>Opaque ciphertext, base64.</summary>
    public required string EncryptedMessageBase64 { get; init; }

    /// <summary>Optional end-to-end correlation id. Relayed unchanged.</summary>
    public string? CorrelationId { get; init; }
}
