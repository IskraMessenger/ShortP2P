namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Bot authorization payload.</summary>
public sealed class BotLoginRequest
{
    /// <summary>Short network id (base64url, ~16 chars).</summary>
    public required string NetworkId { get; init; }

    /// <summary>Bot logical name (max <see cref="BotLimits.MaxBotNameLength"/> chars).</summary>
    public required string BotName { get; init; }

    /// <summary>
    /// Bot key: base64, exactly <see cref="BotLimits.BotKeyLength"/> characters.
    /// Issued by the server on registration; known only to the server and the bot.
    /// <para><b>Secret:</b> must not be logged, relayed to clients, or shared otherwise.</para>
    /// </summary>
    public required string BotKey { get; init; }

    /// <summary>
    /// SHA-256 fingerprint of the bot TLS certificate (hex; optional <c>:</c>/<c>-</c>/space separators).
    /// Must match the fingerprint stored at registration.
    /// </summary>
    public required string FingerprintSha256 { get; init; }

    /// <summary>
    /// Current bot software version. Zero or greater.
    /// Replaces the version stored at registration.
    /// </summary>
    public required int Version { get; init; }

    /// <summary>
    /// Commands from the bot's <c>bot_commands</c> file.
    /// Replaces the command list stored at registration.
    /// </summary>
    public IReadOnlyList<BotCommandDto>? Commands { get; init; }
}
