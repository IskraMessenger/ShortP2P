namespace ShortP2P.MessengerServer.Domain;

/// <summary>Registered bot on this messenger server. The bot key itself is never stored, only its hash.</summary>
public sealed class BotAccount
{
    public required string NetworkId { get; init; }

    public required string BotName { get; init; }

    public required string BotReadableName { get; init; }

    public required string BotDescription { get; init; }

    public required string KeySalt { get; init; }

    public required string KeyHash { get; init; }

    /// <summary>SHA-256 fingerprint of the bot TLS certificate (64 lowercase hex chars).</summary>
    public required string CertificateFingerprintSha256 { get; init; }

    /// <summary>RSA public key JSON for client ↔ bot message encryption (same shape as ChatRequest).</summary>
    public required string MessagePublicKey { get; init; }

    /// <summary>Bot software version reported at registration and refreshed on login.</summary>
    public required int Version { get; set; }

    /// <summary>Commands last reported at registration and refreshed on login. Empty when the bot reported none.</summary>
    public IReadOnlyList<BotCommand> Commands { get; set; } = [];

    /// <summary>Creation time UTC.</summary>
    public required DateTime CreatedAtUtc { get; init; }
}
