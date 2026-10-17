using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ShortP2P.MessengerServer.Domain;

/// <summary>Rules for opaque ids stored as strings (messageId, blobId, forwardId, correlationId).</summary>
public static partial class OpaqueIdRules
{
    public const int MaxMessageIdLength = 128;
    public const int MaxBlobIdLength = 128;
    public const int MaxForwardIdLength = 128;
    public const int MaxCorrelationIdLength = 32;

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeIdAlphabetRegex();

    public static bool IsValid([NotNullWhen(true)] string? value, int maxLength) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= maxLength
        && SafeIdAlphabetRegex().IsMatch(value);

    public static string RequireValid(string? value, int maxLength, string paramName)
    {
        var trimmed = value?.Trim() ?? "";
        if (!IsValid(trimmed, maxLength))
        {
            throw new ArgumentException(
                $"{paramName} must be 1..{maxLength} characters from [A-Za-z0-9_-].",
                paramName);
        }

        return trimmed;
    }
}
