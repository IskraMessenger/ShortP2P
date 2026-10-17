namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Per-bot send_messages rate limit settings.</summary>
public sealed class BotRateLimitOptions
{
    public const string Section = "BotRateLimit";

    public bool Enabled { get; set; } = true;

    /// <summary>BASE_RPS in the RPS_limit formula.</summary>
    public double BaseRps { get; set; } = 10;

    /// <summary>Channel bandwidth in Mbit/s for K_bandwidth (default 1).</summary>
    public double BandwidthMbps { get; set; } = 1;

    /// <summary>
    /// CPU type key for K_type_penalty (e.g. <c>pentium</c>, <c>arm64</c>, <c>ryzen_5</c>).
    /// </summary>
    public string CpuType { get; set; } = "pentium";

    /// <summary>
    /// Default host RAM in GiB for K_ram when hardware probe is unavailable.
    /// </summary>
    public double RamGigabytes { get; set; } = 1;

    /// <summary>Cooldown base seconds; duration = CooldownBaseSeconds × (N − 2).</summary>
    public int CooldownBaseSeconds { get; set; } = 30;

    /// <summary>Consecutive 429 count that starts cooldown (N ≥ this).</summary>
    public int CooldownTriggerCount { get; set; } = 3;
}
