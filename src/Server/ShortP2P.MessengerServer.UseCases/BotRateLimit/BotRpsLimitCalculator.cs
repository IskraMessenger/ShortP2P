namespace ShortP2P.MessengerServer.UseCases.BotRateLimit;

/// <summary>
/// RPS_limit = BASE_RPS × K_cpu × K_ram × K_bandwidth × K_type_penalty.
/// </summary>
public static class BotRpsLimitCalculator
{
    public const double MinRpsLimit = 0.1;

    /// <summary>Floor for K_bandwidth so 1 Mbit/s (log10=0) does not zero the limit.</summary>
    public const double MinBandwidthCoefficient = 0.1;

    public static double Compute(
        double baseRps,
        int logicalCoreCount,
        double totalRamMegabytes,
        double bandwidthMbps,
        string? cpuType)
    {
        if (!double.IsFinite(baseRps) || baseRps <= 0)
            baseRps = 10;

        var kCpu = GetCpuCoefficient(logicalCoreCount);
        var kRam = GetRamCoefficient(totalRamMegabytes / 1024.0);
        var kBandwidth = GetBandwidthCoefficient(bandwidthMbps);
        var kType = BotCpuTypes.ResolvePenalty(cpuType);

        var limit = baseRps * kCpu * kRam * kBandwidth * kType;
        if (!double.IsFinite(limit) || limit < MinRpsLimit)
            return MinRpsLimit;

        return limit;
    }

    /// <summary>K_cpu = log₂(cores) / log₂(8). One core uses the documented ⅓ (log₂(1)=0).</summary>
    public static double GetCpuCoefficient(int coreCount)
    {
        var cores = Math.Max(1, coreCount);
        if (cores == 1)
            return 1.0 / 3.0;

        return Math.Log2(cores) / Math.Log2(8);
    }

    /// <summary>K_ram from RAM size in GiB.</summary>
    public static double GetRamCoefficient(double gigabytes)
    {
        if (!double.IsFinite(gigabytes) || gigabytes < 0.5)
            return 0.3;

        if (gigabytes >= 16)
            return 2.0;

        return Math.Min(0.4 + (gigabytes - 0.5) / 7.5, 2.0);
    }

    /// <summary>K_bandwidth = min(log₁₀(mbps) / log₁₀(10), 4), floored at <see cref="MinBandwidthCoefficient"/>.</summary>
    public static double GetBandwidthCoefficient(double mbps)
    {
        if (!double.IsFinite(mbps) || mbps <= 0)
            return MinBandwidthCoefficient;

        var value = Math.Log10(mbps) / Math.Log10(10.0);
        if (!double.IsFinite(value))
            return MinBandwidthCoefficient;

        return Math.Clamp(value, MinBandwidthCoefficient, 4.0);
    }
}
