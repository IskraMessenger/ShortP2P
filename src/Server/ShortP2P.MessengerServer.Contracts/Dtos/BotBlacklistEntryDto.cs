namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>One blacklisted bot network id.</summary>
public sealed class BotBlacklistEntryDto
{
    /// <summary>Bot short network id (base64url, ~16 chars).</summary>
    public required string NetworkId { get; init; }
}
