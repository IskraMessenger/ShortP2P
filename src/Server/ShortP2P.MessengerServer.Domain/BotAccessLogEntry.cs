namespace ShortP2P.MessengerServer.Domain;

/// <summary>Audit record for bot ↔ server interaction attempts.</summary>
public sealed class BotAccessLogEntry
{
    public required long Id { get; init; }

    /// <summary>UTC timestamp of the attempt.</summary>
    public required DateTime AtUtc { get; init; }

    /// <summary>Bot short network id when known.</summary>
    public required string NetworkId { get; init; }

    /// <summary>Optional bot logical name (register/login).</summary>
    public string? BotName { get; init; }

    /// <summary>Operation name, e.g. register, login, remove, send_messages.</summary>
    public required string Operation { get; init; }

    /// <summary>Outcome code: Succeeded, Forbidden, Unauthorized, Validation, Conflict, …</summary>
    public required string Outcome { get; init; }

    /// <summary>Human-readable detail; must never contain botKey.</summary>
    public required string Detail { get; init; }
}
