namespace ShortP2P.MessengerServer.UseCases.BotTrust;

public sealed class BotTrustOptions
{
    public const string Section = "BotTrust";

    public float DefaultRating { get; set; } = 0.8f;

    public float HighThreshold { get; set; } = 0.8f;

    public float LowThreshold { get; set; } = 0.5f;

    /// <summary>Penalty step G, and quiet-month growth for high-trust bots.</summary>
    public float StepG { get; set; } = 0.05f;

    /// <summary>Quiet-month growth step for middle-trust bots ([0.5, 0.8)).</summary>
    public float MiddleGrowthStepG { get; set; } = 0.025f;

    /// <summary>Floor for reduced G during a claim flood.</summary>
    public float MinFloodStepG { get; set; } = 0.01f;

    /// <summary>Claims from one network id above this count within <see cref="ClaimWindow"/> are fraud.</summary>
    public int MaxClaimsPerComplainantPerWindow { get; set; } = 3;

    /// <summary>Share of bot users that makes unique claims in the window a flood (10%).</summary>
    public float FloodUserShareThreshold { get; set; } = 0.10f;

    public TimeSpan ClaimWindow { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Quiet period after publication (or last claim) before monthly growth starts.</summary>
    public TimeSpan GrowthPeriod { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan AntifraudPollInterval { get; set; } = TimeSpan.FromMinutes(1);
}
