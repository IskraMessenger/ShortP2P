namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>New bot registration payload.</summary>
public sealed class BotRegisterRequest
{
    /// <summary>Short network id (base64url, ~16 chars).</summary>
    public required string NetworkId { get; init; }

    /// <summary>Bot logical name (max <see cref="BotLimits.MaxBotNameLength"/> chars).</summary>
    public required string BotName { get; init; }

    /// <summary>Human-readable display name (max <see cref="BotLimits.MaxBotReadableNameLength"/> chars).</summary>
    public required string BotReadableName { get; init; }

    /// <summary>Bot description (max <see cref="BotLimits.MaxBotDescriptionLength"/> chars).</summary>
    public required string BotDescription { get; init; }

    /// <summary>
    /// SHA-256 fingerprint of the bot TLS certificate (hex; optional <c>:</c>/<c>-</c>/space separators).
    /// Stored by the server and checked on later <see cref="BotLoginRequest"/>.
    /// </summary>
    public required string FingerprintSha256 { get; init; }

    /// <summary>
    /// RSA public key JSON for client ↔ bot message encryption (same shape as ChatRequest <c>publicKey</c>).
    /// Exposed to authorized clients via <see cref="BotCatalogDto"/>.
    /// </summary>
    public required string MessagePublicKey { get; init; }

    /// <summary>Bot software version. Zero or greater.</summary>
    public required int Version { get; init; }
}
