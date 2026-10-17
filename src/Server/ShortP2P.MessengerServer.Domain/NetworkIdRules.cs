using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ShortP2P.MessengerServer.Domain;

/// <summary>
/// Wire/DB rules for short network ids (base64url alphabet; production ids are ~16 chars from 12 wire bytes).
/// </summary>
public static partial class NetworkIdRules
{
    public const int MaxLength = 64;

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Base64UrlAlphabetRegex();

    public static bool IsValid([NotNullWhen(true)] string? networkId) =>
        !string.IsNullOrEmpty(networkId)
        && networkId.Length <= MaxLength
        && Base64UrlAlphabetRegex().IsMatch(networkId);

    public static string RequireValid(string? networkId, string paramName = "networkId")
    {
        var trimmed = networkId?.Trim() ?? "";
        if (!IsValid(trimmed))
        {
            throw new ArgumentException(
                $"NetworkId must be 1..{MaxLength} base64url characters (A-Z, a-z, 0-9, _, -).",
                paramName);
        }

        return trimmed;
    }
}
