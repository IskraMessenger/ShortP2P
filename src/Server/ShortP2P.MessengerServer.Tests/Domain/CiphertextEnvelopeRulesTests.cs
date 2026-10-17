using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Tests.Domain;

public class CiphertextEnvelopeRulesTests
{
    [Fact]
    public void TryValidate_AcceptsValidBase64WithinLimit()
    {
        Assert.True(CiphertextEnvelopeRules.TryValidate("Y2lwaGVy", 1024, out var normalized, out var len));
        Assert.Equal("Y2lwaGVy", normalized);
        Assert.Equal(6, len);
    }

    [Theory]
    [InlineData("not-base64!!!")]
    [InlineData("")]
    [InlineData(null)]
    public void TryValidate_RejectsInvalid(string? value) =>
        Assert.False(CiphertextEnvelopeRules.TryValidate(value, 1024, out _, out _));

    [Fact]
    public void TryValidate_RejectsOversizedDecodedPayload()
    {
        var huge = Convert.ToBase64String(new byte[8]);
        Assert.False(CiphertextEnvelopeRules.TryValidate(huge, maxDecodedBytes: 4, out _, out _));
    }
}
