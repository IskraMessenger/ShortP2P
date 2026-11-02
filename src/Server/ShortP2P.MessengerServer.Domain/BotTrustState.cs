namespace ShortP2P.MessengerServer.Domain;

/// <summary>Per-bot trust score and claim history on this messenger server.</summary>
public sealed class BotTrustState
{
    public required string BotNetworkId { get; set; }

    /// <summary>Trust score in [0, 1]. Default at publication is 0.8.</summary>
    public float Rating { get; set; }

    /// <summary>When the bot was published / trust row created (UTC).</summary>
    public required DateTime PublishedAtUtc { get; set; }

    /// <summary>Last time a claim was ingested (UTC).</summary>
    public DateTime? LastClaimUtc { get; set; }

    /// <summary>Full months of quiet growth already applied since publication / last claim.</summary>
    public int GrowthMonthsApplied { get; set; }

    /// <summary>Complainant network ids that already reduced the rating (unique lifetime penalties).</summary>
    public List<string> PenalizedComplainants { get; set; } = [];

    /// <summary>Known users of this bot on this server (clients who messaged or claimed).</summary>
    public List<string> UserNetworkIds { get; set; } = [];

    public List<BotClaimRecord> Claims { get; set; } = [];

    public BotTrustState Clone()
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
