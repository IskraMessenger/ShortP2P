using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ShortP2P.Client.Services;

/// <summary>
///     Hidden session-coordination text frames (not stored in chat history).
///     Format:
///     <c>networkId1:networkId2:test_send|test_response_for_&lt;hash&gt;:test:datetime:sha512hex</c>
///     where SHA-512 covers everything before the final <c>:hash</c> field.
/// </summary>
public static class SessionCryptoProbe
{
    public const string KindTestSend = "test_send";
    public const string KindTestResponsePrefix = "test_response_for_";
    public const string LiteralTest = "test";
    private const string TestSeparator = ":test:";

    /// <summary>Builds a <c>test_send</c> frame; <paramref name="contentHash"/> is SHA-512 of the payload without the hash field.</summary>
    public static string FormatTestSend(string sourcePeerIdShort, string targetPeerIdShort, DateTimeOffset utc,
        out string contentHash)
    {
        var datetime = FormatDateTime(utc);
        var withoutHash = JoinWithoutHash(sourcePeerIdShort, targetPeerIdShort, KindTestSend, datetime);
        contentHash = ComputeSha512Hex(withoutHash);
        return withoutHash + ":" + contentHash;
    }

    /// <summary>Builds a <c>test_response_for_&lt;testSendHash&gt;</c> frame for a received test_send hash.</summary>
    public static string FormatTestResponse(string sourcePeerIdShort, string targetPeerIdShort,
        string testSendHash, DateTimeOffset utc, out string contentHash)
    {
        Require.NotNullOrWhiteSpace(testSendHash);
        var kind = KindTestResponsePrefix + testSendHash.Trim();
        var datetime = FormatDateTime(utc);
        var withoutHash = JoinWithoutHash(sourcePeerIdShort, targetPeerIdShort, kind, datetime);
        contentHash = ComputeSha512Hex(withoutHash);
        return withoutHash + ":" + contentHash;
    }

    public static bool TryParse(string text, out SessionCryptoProbeMessage message)
    {
        message = default!;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        var sep = text.IndexOf(TestSeparator, StringComparison.Ordinal);
        if (sep <= 0)
            return false;

        var left = text[..sep];
        var right = text[(sep + TestSeparator.Length)..];
        if (left.Length == 0 || right.Length == 0)
            return false;

        var c1 = left.IndexOf(':');
        if (c1 <= 0)
            return false;
        var c2 = left.IndexOf(':', c1 + 1);
        if (c2 <= c1 + 1 || c2 >= left.Length - 1)
            return false;

        var src = left[..c1].Trim();
        var tgt = left[(c1 + 1)..c2].Trim();
        var kind = left[(c2 + 1)..].Trim();
        if (src.Length == 0 || tgt.Length == 0 || kind.Length == 0)
            return false;

        var lastColon = right.LastIndexOf(':');
        if (lastColon <= 0 || lastColon >= right.Length - 1)
            return false;

        var datetime = right[..lastColon].Trim();
        var hash = right[(lastColon + 1)..].Trim();
        if (datetime.Length == 0 || hash.Length == 0)
            return false;

        SessionCryptoProbeKind probeKind;
        string? referencedSendHash = null;
        if (string.Equals(kind, KindTestSend, StringComparison.Ordinal))
        {
            probeKind = SessionCryptoProbeKind.TestSend;
        }
        else if (kind.StartsWith(KindTestResponsePrefix, StringComparison.Ordinal) &&
                 kind.Length > KindTestResponsePrefix.Length)
        {
            probeKind = SessionCryptoProbeKind.TestResponse;
            referencedSendHash = kind[KindTestResponsePrefix.Length..];
        }
        else
            return false;

        var withoutHash = JoinWithoutHash(src, tgt, kind, datetime);
        var expectedHash = ComputeSha512Hex(withoutHash);
        if (!FixedTimeEqualsHex(expectedHash, hash))
            return false;

        message = new SessionCryptoProbeMessage(probeKind, src, tgt, kind, datetime, hash, referencedSendHash);
        return true;
    }

    /// <summary>True when the text is a session test frame that must not appear in chat UI.</summary>
    public static bool LooksLikeTestMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        if (TryParse(text, out _))
            return true;

        text = text.Trim();
        var sep = text.IndexOf(TestSeparator, StringComparison.Ordinal);
        if (sep <= 0)
            return false;
        var left = text[..sep];
        var c1 = left.IndexOf(':');
        if (c1 <= 0)
            return false;
        var c2 = left.IndexOf(':', c1 + 1);
        if (c2 <= c1 + 1 || c2 >= left.Length - 1)
            return false;
        var kind = left[(c2 + 1)..].Trim();
        return string.Equals(kind, KindTestSend, StringComparison.Ordinal) ||
               kind.StartsWith(KindTestResponsePrefix, StringComparison.Ordinal);
    }

    public static string ComputeSha512Hex(string payloadWithoutHash)
    {
        Require.NotNull(payloadWithoutHash);
        var bytes = Encoding.UTF8.GetBytes(payloadWithoutHash);
        var hash = SHA512.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string JoinWithoutHash(string src, string tgt, string kind, string datetime) =>
        $"{src.Trim()}:{tgt.Trim()}:{kind}:{LiteralTest}:{datetime}";

    private static string FormatDateTime(DateTimeOffset utc) =>
        utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    private static bool FixedTimeEqualsHex(string a, string b)
    {
        if (a.Length != b.Length)
            return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= char.ToLowerInvariant(a[i]) ^ char.ToLowerInvariant(b[i]);
        return diff == 0;
    }
}

public enum SessionCryptoProbeKind
{
    TestSend,
    TestResponse
}

public sealed class SessionCryptoProbeMessage(
    SessionCryptoProbeKind kind,
    string sourceNetworkId,
    string targetNetworkId,
    string kindToken,
    string datetime,
    string contentHash,
    string? referencedTestSendHash)
{
    public SessionCryptoProbeKind Kind { get; } = kind;
    public string SourceNetworkId { get; } = sourceNetworkId;
    public string TargetNetworkId { get; } = targetNetworkId;
    public string KindToken { get; } = kindToken;
    public string DateTime { get; } = datetime;
    public string ContentHash { get; } = contentHash;
    public string? ReferencedTestSendHash { get; } = referencedTestSendHash;
}
