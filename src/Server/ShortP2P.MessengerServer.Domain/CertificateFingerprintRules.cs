using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace ShortP2P.MessengerServer.Domain;

/// <summary>Wire/DB rules for SHA-256 certificate fingerprints (hex, optional separators).</summary>
public static partial class CertificateFingerprintRules
{
    public const int HexLength = 64;

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexSha256Regex();

    /// <summary>Strips <c>:</c>, spaces and dashes; lowercases hex digits.</summary>
    public static string Normalize(string? fingerprint)
    {
        if (fingerprint is null || string.IsNullOrWhiteSpace(fingerprint))
            return "";

        var sb = new StringBuilder(fingerprint.Length);
        foreach (var c in fingerprint.Trim())
        {
            if (c is ':' or ' ' or '-')
                continue;
            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    public static bool IsValid([NotNullWhen(true)] string? fingerprint)
    {
        var normalized = Normalize(fingerprint);
        return normalized.Length == HexLength && HexSha256Regex().IsMatch(normalized);
    }

    public static bool AreEqual(string? a, string? b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal);
}
