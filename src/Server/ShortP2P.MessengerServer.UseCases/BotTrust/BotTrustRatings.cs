using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.BotTrust;

/// <summary>
/// Fixed bot-trust coefficients. Not configurable via appsettings.
/// </summary>
public static class BotTrustRatings
{
    /// <summary>Initial rating when a bot is published on this server.</summary>
    public const float Default = 0.8f;

    /// <summary>High band: rating &gt;= this value.</summary>
    public const float HighThreshold = 0.8f;

    /// <summary>Low band (banned): rating &lt; this value. Middle is [Low, High).</summary>
    public const float LowThreshold = 0.5f;

    /// <summary>Unique-claim penalty and quiet-month growth for high trust.</summary>
    public const float StepG = 0.05f;

    /// <summary>Quiet-month growth for middle trust.</summary>
    public const float MiddleGrowthStepG = 0.025f;

    /// <summary>Floor for reduced G during a claim flood.</summary>
    public const float MinFloodStepG = 0.01f;

    /// <summary>More claims than this from one network id in <see cref="ClaimWindow"/> is fraud.</summary>
    public const int MaxClaimsPerComplainantPerWindow = 3;

    /// <summary>Share of bot users that makes unique claims in the window a flood (10%).</summary>
    public const float FloodUserShareThreshold = 0.10f;

    /// <summary>Claim scoring window length.</summary>
    public static readonly TimeSpan ClaimWindow = TimeSpan.FromHours(1);

    /// <summary>Quiet period before one growth step applies.</summary>
    public static readonly TimeSpan GrowthPeriod = TimeSpan.FromDays(30);

    public static BotTrustLevel Classify(float rating) =>
        rating switch
        {
            >= HighThreshold => BotTrustLevel.High,
            >= LowThreshold => BotTrustLevel.Middle,
            _ => BotTrustLevel.Low
        };

    public static bool IsBanned(float rating) => Classify(rating) == BotTrustLevel.Low;

    public static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
}
