namespace ShortP2P.MessengerServer.UseCases.BotRateLimit;

/// <summary>Named CPU type → K_type_penalty multipliers.</summary>
public static class BotCpuTypes
{
    public const double DefaultPenalty = 1.0;

    private static readonly Dictionary<string, double> Penalties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["celeron"] = 0.6,
        ["pentium"] = 0.75,
        ["pentium_4"] = 0.75,
        ["arm64"] = 0.8,
        ["xeon_e5"] = 0.95,
        ["ryzen_5"] = 1.0,
        ["ryzen_7"] = 1.15,
        ["ryzen_9"] = 1.25,
        ["xeon_platinum"] = 1.3
    };

    public static double ResolvePenalty(string? cpuType)
    {
        if (string.IsNullOrWhiteSpace(cpuType))
            return DefaultPenalty;

        return Penalties.TryGetValue(cpuType.Trim(), out var penalty)
            ? penalty
            : DefaultPenalty;
    }
}
