namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Encrypted message from a bot to a client (server does not decrypt the payload).</summary>
public sealed class BotClientInboundMessageDto
{
    /// <summary>Message id (unique per inbound message).</summary>
    public required string MessageId { get; init; }

    /// <summary>Bot short network id (base64url, ~16 chars).</summary>
    public required string BotNetworkId { get; init; }

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
