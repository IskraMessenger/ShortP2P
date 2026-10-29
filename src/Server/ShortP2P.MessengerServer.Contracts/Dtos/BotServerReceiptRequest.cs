namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Bot confirms it received a client → bot message. The server then drops the message and its chunks.</summary>
public sealed class BotServerReceiptRequest
{
    public required string RequestId { get; init; }

    /// <summary>Bot short network id (base64url, ~16 chars).</summary>
    public required string BotNetworkId { get; init; }

    /// <summary>
    /// Bot key: base64, exactly <see cref="BotLimits.BotKeyLength"/> characters.
    /// <para><b>Secret:</b> known only to the server and the bot.</para>
    /// </summary>
    public required string BotKey { get; init; }

    public required string MessageId { get; init; }
}
