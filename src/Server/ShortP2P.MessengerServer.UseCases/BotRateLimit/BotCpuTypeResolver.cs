using System.Text;

namespace ShortP2P.MessengerServer.UseCases.BotRateLimit;

/// <summary>Resolves configured / Auto CPU type and core count for bot RPS limits.</summary>
public static class BotCpuTypeResolver
{
    public const string Auto = "Auto";
    public const string FallbackCpuType = "pentium";
    public const int FallbackCoreCount = 2;

    public static bool IsAuto(string? cpuType) =>
        string.Equals(cpuType?.Trim(), Auto, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// When <paramref name="configuredCpuType"/> is Auto, map brand heuristics (else <see cref="FallbackCpuType"/>).
    /// Otherwise return the configured key (trimmed).
    /// </summary>
    public static string ResolveCpuType(string? configuredCpuType, string? cpuBrandName)
    {
        if (!IsAuto(configuredCpuType))
            return string.IsNullOrWhiteSpace(configuredCpuType)
                ? FallbackCpuType
                : configuredCpuType.Trim();

        return ResolveFromBrand(cpuBrandName);
    }

    public static int ResolveCoreCount(int? logicalCoreCount) =>
        logicalCoreCount is > 0 ? logicalCoreCount.Value : FallbackCoreCount;

    /// <summary>Map a CPU brand/name string to a <see cref="BotCpuTypes"/> key.</summary>
    public static string ResolveFromBrand(string? cpuBrandName)
    {
        if (string.IsNullOrWhiteSpace(cpuBrandName))
            return FallbackCpuType;

        var brand = NormalizeBrand(cpuBrandName);

        // More specific keywords first.
        if (Contains(brand, "xeon") && Contains(brand, "platinum"))
            return "xeon_platinum";
        if (Contains(brand, "xeon") && ContainsToken(brand, "e5"))
            return "xeon_e5";
        if (Contains(brand, "ryzen") && ContainsToken(brand, "9"))
            return "ryzen_9";
        if (Contains(brand, "ryzen") && ContainsToken(brand, "7"))
            return "ryzen_7";
        if (Contains(brand, "ryzen") && ContainsToken(brand, "5"))
            return "ryzen_5";
        if (Contains(brand, "pentium") && ContainsToken(brand, "4"))
            return "pentium_4";
        if (Contains(brand, "celeron"))
            return "celeron";
        if (Contains(brand, "pentium"))
            return "pentium";
        if (Contains(brand, "aarch64") ||
            Contains(brand, "arm64") ||
            Contains(brand, "arm") ||
            Contains(brand, "apple m"))
            return "arm64";

        return FallbackCpuType;
    }

    private static string NormalizeBrand(string brand)
    {
        var trimmed = brand.Trim();
        var sb = new StringBuilder(trimmed.Length);
        for (var i = 0; i < trimmed.Length; i++)
        {
            // Drop common trademark wrappers: (R), (TM)
            if (trimmed[i] == '(' && i + 2 < trimmed.Length)
            {
                var close = trimmed.IndexOf(')', i + 1);
                if (close > i)
                {
                    var inner = trimmed[(i + 1)..close];
                    if (inner.Equals("R", StringComparison.OrdinalIgnoreCase) ||
                        inner.Equals("TM", StringComparison.OrdinalIgnoreCase))
                    {
                        i = close;
                        continue;
                    }
                }
            }

            var ch = trimmed[i];
            if (ch is '®' or '™')
                continue;

            sb.Append(char.ToLowerInvariant(ch));
        }

        var normalized = sb.ToString();
        while (normalized.Contains("  ", StringComparison.Ordinal))
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);

        return normalized.Trim();
    }

    private static bool Contains(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.Ordinal);

    /// <summary>Match a whole token (letter/digit run), e.g. "e5" in "cpu e5-2680".</summary>
    private static bool ContainsToken(string haystack, string token)
    {
        var start = 0;
        while (start < haystack.Length)
        {
            while (start < haystack.Length && !IsTokenChar(haystack[start]))
                start++;
            if (start >= haystack.Length)
                break;

            var end = start;
            while (end < haystack.Length && IsTokenChar(haystack[end]))
                end++;

            if (end - start == token.Length &&
                string.Compare(haystack, start, token, 0, token.Length, StringComparison.Ordinal) == 0)
                return true;

            start = end;
        }

        return false;
    }

    private static bool IsTokenChar(char ch) => char.IsLetterOrDigit(ch);
}
