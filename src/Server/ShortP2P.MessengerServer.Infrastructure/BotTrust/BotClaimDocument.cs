namespace ShortP2P.MessengerServer.Infrastructure.BotTrust;

public sealed class BotClaimDocument
{
    public string ComplainantNetworkId { get; set; } = "";

    public DateTime Utc { get; set; }

    public bool Processed { get; set; }
}
