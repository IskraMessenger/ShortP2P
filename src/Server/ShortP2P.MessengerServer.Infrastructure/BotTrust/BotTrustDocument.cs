using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Infrastructure.BotTrust;

public sealed class BotTrustDocument
{
    public string Id { get; set; } = "";

    public string BotNetworkId { get; set; } = "";

    public float Rating { get; set; }

    public DateTime PublishedAtUtc { get; set; }

    public DateTime? LastClaimUtc { get; set; }

    public int GrowthMonthsApplied { get; set; }

    public List<string> PenalizedComplainants { get; set; } = [];

    public List<string> UserNetworkIds { get; set; } = [];

    public List<BotClaimDocument> Claims { get; set; } = [];

    public static BotTrustDocument FromDomain(BotTrustState state)
    {
        var id = state.BotNetworkId.Trim();
        return new BotTrustDocument
        {
            Id = id,
            BotNetworkId = id,
            Rating = state.Rating,
            PublishedAtUtc = state.PublishedAtUtc,
            LastClaimUtc = state.LastClaimUtc,
            GrowthMonthsApplied = state.GrowthMonthsApplied,
            PenalizedComplainants = state.PenalizedComplainants.ToList(),
            UserNetworkIds = state.UserNetworkIds.ToList(),
            Claims = state.Claims.Select(c => new BotClaimDocument
            {
                ComplainantNetworkId = c.ComplainantNetworkId,
                Utc = c.Utc,
                Processed = c.Processed
            }).ToList()
        };
    }

    public BotTrustState ToDomain()
    {
        return new BotTrustState
        {
            BotNetworkId = BotNetworkId,
            Rating = Rating,
            PublishedAtUtc = PublishedAtUtc,
            LastClaimUtc = LastClaimUtc,
            GrowthMonthsApplied = GrowthMonthsApplied,
            PenalizedComplainants = PenalizedComplainants.ToList(),
            UserNetworkIds = UserNetworkIds.ToList(),
            Claims = Claims.Select(c => new BotClaimRecord
            {
                ComplainantNetworkId = c.ComplainantNetworkId,
                Utc = c.Utc,
                Processed = c.Processed
            }).ToList()
        };
    }
}
