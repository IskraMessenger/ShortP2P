using System.Diagnostics.CodeAnalysis;

namespace ShortP2P.MessengerServer.Domain;

/// <summary>
/// Rules for server-visible cleartext (nick, bot names, descriptions, log detail).
/// Blocks control characters and HTML angle brackets that enable XSS / log forging.
/// </summary>
public static class CleartextFieldRules
{
    public const int MaxNickLength = 256;

    public static bool IsValid([NotNullWhen(true)] string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length > maxLength)
            return false;

        foreach (var ch in value)
        {
            if (ch is < (char)0x20 or (char)0x7F or '<' or '>')
                return false;
        }

        return true;
    }

    /// <summary>Trims and strips disallowed characters for stored/log fields that must not fail hard.</summary>
    public static string Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var trimmed = value.Trim();
        var buffer = trimmed.Length <= 1024
            ? stackalloc char[trimmed.Length]
            : new char[trimmed.Length];
        var n = 0;
        
        foreach (var ch in trimmed.Where(ch => ch is not (< (char)0x20 or (char)0x7F or '<' or '>')))
        {
            buffer[n++] = ch;
            if (n >= maxLength)
                break;
        }

        return n == 0 ? string.Empty : new string(buffer[..n]);
    }
}
