using SQLite;

namespace ShortP2P.Client.Data;

/// <summary>
/// Bot version learned from one messenger server.
/// The same bot network id can report a different version on another server.
/// </summary>
[Table("bot_versions")]
public sealed class BotVersionEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Local <see cref="MessengerServerEntity.Id"/>.</summary>
    [Indexed]
    public int ServerId { get; set; }

    public string BotNetworkId { get; set; } = "";

    public int Version { get; set; }

    /// <summary>Host and port from the server base URL (<c>host:port</c>, IPv6 as <c>[host]:port</c>).</summary>
    public string Endpoint { get; set; } = "";

    /// <summary>Pinned SHA-256 fingerprint of the server TLS certificate.</summary>
    public string FingerprintSha256 { get; set; } = "";

    public long UpdatedUtcTicks { get; set; }
}
