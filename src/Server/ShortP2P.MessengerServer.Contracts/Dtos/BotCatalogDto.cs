namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Public bot card for client selection. Keys and certificate material are not included.</summary>
public sealed class BotCatalogDto
{
    public required string NetworkId { get; init; }

    public required string BotName { get; init; }

    public required string BotReadableName { get; init; }

    public required string BotDescription { get; init; }

    /// <summary>RSA public key JSON for encrypting client → bot payloads.</summary>
    /// <remarks>Not <c>required</c>: older servers / partial JSON must deserialize; empty is filtered by list.</remarks>
    public string MessagePublicKey { get; init; } = "";
}
