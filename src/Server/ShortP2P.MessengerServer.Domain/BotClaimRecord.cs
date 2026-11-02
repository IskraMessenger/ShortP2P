namespace ShortP2P.MessengerServer.Domain;

/// <summary>One client complaint about a bot on this messenger server.</summary>
public sealed class BotClaimRecord
{
    public required string ComplainantNetworkId { get; set; }

    public required DateTime Utc { get; set; }

    /// <summary>True after the antifraud hosted service folded this claim into the rating.</summary>
    public bool Processed { get; set; }
}
