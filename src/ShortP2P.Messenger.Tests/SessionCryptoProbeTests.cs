using ShortP2P.Client.Services;

namespace ShortP2P.Messenger.Tests;

public class SessionCryptoProbeTests
{
    [Fact]
    public void FormatTestSend_RoundTrips_WithValidSha512()
    {
        var utc = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        var frame = SessionCryptoProbe.FormatTestSend("nidA", "nidB", utc, out var hash);

        Assert.Equal(
            $"nidA:nidB:test_send:test:2026-10-06T12:00:00.000Z:{hash}",
            frame);
        Assert.True(SessionCryptoProbe.TryParse(frame, out var msg));
        Assert.Equal(SessionCryptoProbeKind.TestSend, msg.Kind);
        Assert.Equal("nidA", msg.SourceNetworkId);
        Assert.Equal("nidB", msg.TargetNetworkId);
        Assert.Equal(hash, msg.ContentHash);
        Assert.Null(msg.ReferencedTestSendHash);
    }

    [Fact]
    public void FormatTestResponse_ReferencesSendHash_AndValidates()
    {
        var utc = new DateTimeOffset(2026, 10, 6, 12, 1, 0, TimeSpan.Zero);
        var sendHash = "deadbeefcafebabe";
        var frame = SessionCryptoProbe.FormatTestResponse("nidB", "nidA", sendHash, utc, out var hash);

        Assert.StartsWith($"nidB:nidA:test_response_for_{sendHash}:test:", frame, StringComparison.Ordinal);
        Assert.EndsWith(":" + hash, frame, StringComparison.Ordinal);
        Assert.True(SessionCryptoProbe.TryParse(frame, out var msg));
        Assert.Equal(SessionCryptoProbeKind.TestResponse, msg.Kind);
        Assert.Equal(sendHash, msg.ReferencedTestSendHash);
        Assert.Equal(hash, msg.ContentHash);
    }

    [Fact]
    public void TryParse_RejectsTamperedHash()
    {
        var frame = SessionCryptoProbe.FormatTestSend("a", "b", DateTimeOffset.UtcNow, out _);
        var tampered = frame[..^4] + "0000";
        Assert.False(SessionCryptoProbe.TryParse(tampered, out _));
        Assert.True(SessionCryptoProbe.LooksLikeTestMessage(tampered));
    }

    [Fact]
    public void LooksLikeTestMessage_DetectsKindWithoutFullParse()
    {
        Assert.True(SessionCryptoProbe.LooksLikeTestMessage(
            "x:y:test_send:test:2026-01-01T00:00:00.000Z:notahash"));
        Assert.True(SessionCryptoProbe.LooksLikeTestMessage(
            "x:y:test_response_for_abc:test:2026-01-01T00:00:00.000Z:notahash"));
        Assert.False(SessionCryptoProbe.LooksLikeTestMessage("hello chat"));
    }
}
