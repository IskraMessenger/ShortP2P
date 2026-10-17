using System.Diagnostics.CodeAnalysis;

namespace ShortP2P.MessengerServer.Domain;

/// <summary>
/// Validates opaque ciphertext envelopes (base64) without interpreting payload contents.
/// </summary>
public static class CiphertextEnvelopeRules
{
    public static bool TryValidate(
        string? base64,
        int maxDecodedBytes,
        [NotNullWhen(true)] out string? normalized,
        out int decodedLength)
    {
        normalized = null;
        decodedLength = 0;
        if (string.IsNullOrWhiteSpace(base64))
            return false;

        var trimmed = base64.Trim();
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(trimmed);
        }
        catch (FormatException)
        {
            return false;
        }

        if (bytes.Length == 0 || bytes.Length > maxDecodedBytes)
            return false;

        normalized = trimmed;
        decodedLength = bytes.Length;
        return true;
    }

    public static string RequireValid(string? base64, int maxDecodedBytes, string paramName = "ciphertext")
    {
        if (!TryValidate(base64, maxDecodedBytes, out var normalized, out _))
        {
            throw new ArgumentException(
                $"{paramName} must be non-empty valid base64 of at most {maxDecodedBytes} decoded bytes.",
                paramName);
        }

        return normalized;
    }
}
