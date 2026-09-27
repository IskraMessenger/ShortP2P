namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Client → bot: deliver one encrypted message (store-and-forward).</summary>
public sealed class BotSendMessageRequest
{
    /// <summary>Message id (unique per outbound message).</summary>
    public required string MessageId { get; init; }

    /// <summary>Destination bot short network id (base64url, ~16 chars).</summary>
    public required string NetworkId { get; init; }

    /// <summary>Message timestamp (UTC).</summary>
    public required DateTime DateTime { get; init; }

    /// <summary>Opaque ciphertext, base64.</summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional end-to-end correlation id (client ↔ bot); max <see cref="BotLimits.MaxCorrelationIdLength"/> chars;
    /// relayed unchanged by the server.
    /// </summary>
    public string? CorrelationId { get; init; }
}
