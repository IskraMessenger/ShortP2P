namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Add a bot network id to the server blacklist.</summary>
public sealed class AddBotToBlacklistRequest
{
    /// <summary>Bot short network id (base64url, ~16 chars).</summary>
    public required string NetworkId { get; init; }
}
